// Based upon C# code by Sergiy Sakharov (sakharov@gmail.com)
// http://code.google.com/p/dot-net-coverage/source/browse/trunk/Coverage.Counter/Coverage.Counter.csproj

namespace AltCover.Recorder

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Resources
open System.Runtime.CompilerServices

#if NETSTANDARD2_0
[<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
#else
[<System.Runtime.InteropServices.ProgIdAttribute("ExcludeFromCodeCoverage hack for OpenCover issue 615")>]
#endif
type internal Carrier = SequencePoint of String * int * Track

module Instance =
  let internal resources =
    ResourceManager("AltCover.Recorder.Strings", Assembly.GetExecutingAssembly())

  let GetResource s =
    [ System.Globalization.CultureInfo.CurrentUICulture.Name;
      System.Globalization.CultureInfo.CurrentUICulture.Parent.Name; "en" ]
    |> Seq.map (fun l -> resources.GetString(s + "." + l))
    |> Seq.tryFind (String.IsNullOrEmpty >> not)

  /// <summary>
  /// Gets the location of coverage xml file
  /// This property's IL code is modified to store actual file location
  /// </summary>
  [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
  let ReportFile = "Coverage.Default.xml"

  /// <summary>
  /// Accumulation of visit records
  /// </summary>
  let internal Visits = new Dictionary<string, Dictionary<int, int * Track list>>()
  let internal Samples = new Dictionary<string, Dictionary<int, bool>>()

  /// <summary>
  /// Gets the unique token for this instance
  /// This property's IL code is modified to store a GUID-based token
  /// </summary>
  [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
  let Token = "AltCover"

  /// <summary>
  /// Gets the style of the associated report
  /// This property's IL code is modified to store the user chosen override if applicable
  /// </summary>
  [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
  let internal CoverageFormat = ReportFormat.NCover

  /// <summary>
  /// Gets the frequency of time sampling
  /// This property's IL code is modified to store the user chosen override if applicable
  /// </summary>
  [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
  let Timer = 0L

  /// <summary>
  /// Gets the sampling strategy
  /// This property's IL code is modified to store the user chosen override if applicable
  /// </summary>
  [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
  let internal Sample = Sampling.All

  /// <summary>
  /// Gets or sets the current test method
  /// </summary>
  type private CallStack =
    [<ThreadStatic; DefaultValue>]
    static val mutable private instance : Option<CallStack>
    val mutable private caller : int list
    private new(x : int) = { caller = [ x ] }

    static member Instance =
      match CallStack.instance with
      | None -> CallStack.instance <- Some(CallStack(0))
      | _ -> ()
      CallStack.instance.Value

    member self.Push x = self.caller <- x :: self.caller

    //let s = sprintf "push %d -> %A" x self.caller
    //System.Diagnostics.Debug.WriteLine(s)
    member self.Pop() =
      self.caller <- match self.caller with
                     | [] | [ 0 ] -> [ 0 ]
                     | _ :: xs -> xs

    //let s = sprintf "pop -> %A"self.caller
    //System.Diagnostics.Debug.WriteLine(s)
    member self.CallerId() = Seq.head self.caller

  (*let x = Seq.head self.caller
                              let s = sprintf "peek %d" x
                              System.Diagnostics.Debug.WriteLine(s)
                              x*)

  let Push x = CallStack.Instance.Push x
  let Pop() = CallStack.Instance.Pop()
  let CallerId() = CallStack.Instance.CallerId()

  /// <summary>
  /// Serialize access to the report file across AppDomains for the classic mode
  /// </summary>
  let internal mutex = new System.Threading.Mutex(false, Token + ".mutex")

  let SignalFile() = ReportFile + ".acv"

  let internal WithMutex(f : bool -> 'a) =
    let own = mutex.WaitOne(1000)
    try
      f (own)
    finally
      if own then mutex.ReleaseMutex()

  /// <summary>
  /// Reporting back to the mother-ship
  /// </summary>
  type internal TraceOut (dummy:string) =
    static let mutable toFile = false

    static member ToFile
        with get() = toFile
        and set(v) = toFile <- v

    [<ThreadStatic; DefaultValue>]
    static val mutable private instance : Option<Tracer>

    static member private InitialiseTrace() =
      WithMutex(fun _ ->
        let t = Tracer.Create(SignalFile())
        t.OnStart())

    static member Instance =
      match TraceOut.instance with
      | None -> WithMutex(fun _ -> TraceOut.instance <- Some <| TraceOut.InitialiseTrace())
      | _ -> if TraceOut.ToFile && (not <| TraceOut.instance.Value.IsConnected())
             then WithMutex(fun _ -> TraceOut.instance <- Some <| TraceOut.InitialiseTrace())
      TraceOut.instance.Value

    static member Override t =
      TraceOut.instance <- Some t

    override self.ToString() = dummy

  let internal Watcher = new FileSystemWatcher()
  let mutable internal Recording = true

  /// <summary>
  /// This method flushes hit count buffers.
  /// </summary>
  let internal FlushAll finish =
    TraceOut.Instance.OnConnected (fun () -> TraceOut.Instance.OnFinish finish Visits)
      (fun () ->
      match Visits.Count with
      | 0 -> ()
      | _ ->
        let counts = Dictionary<string, Dictionary<int, int * Track list>> Visits
        Visits.Clear()
        WithMutex
          (fun own ->
          let delta =
            Counter.DoFlush ignore (fun _ _ -> ()) own counts CoverageFormat ReportFile
              None
          GetResource "Coverage statistics flushing took {0:N} seconds"
          |> Option.iter (fun s -> Console.Out.WriteLine(s, delta.TotalSeconds))))

  let FlushPause() =
    ("PauseHandler")
    |> GetResource
    |> Option.iter Console.Out.WriteLine
    FlushAll Pause
    Recording <- false

  let FlushResume() =
    Recording <- true
    ("ResumeHandler")
    |> GetResource
    |> Option.iter Console.Out.WriteLine
    Visits.Clear()
    TraceOut.ToFile <- true

  let internal TraceVisit moduleId hitPointId context =
    printfn "TraceVisit %s %d" moduleId hitPointId
    TraceOut.Instance.OnVisit Visits moduleId hitPointId context

  let internal AddVisit moduleId hitPointId context =
    Counter.AddVisit Visits moduleId hitPointId context

  let internal TakeSample strategy moduleId hitPointId =
    match strategy with
    | Sampling.All -> true
    | _ ->
      let hasKey = Samples.ContainsKey(moduleId)
      if hasKey |> not then Samples.Add(moduleId, Dictionary<int, bool>())
      let unwanted = hasKey && Samples.[moduleId].ContainsKey(hitPointId)
      let wanted = unwanted |> not
      if wanted then Samples.[moduleId].Add(hitPointId, true)
      wanted

  /// <summary>
  /// This method is executed from instrumented assemblies.
  /// </summary>
  /// <param name="moduleId">Assembly being visited</param>
  /// <param name="hitPointId">Sequence Point identifier</param>
  let internal VisitImpl moduleId hitPointId context =
    if not <| String.IsNullOrEmpty(moduleId) &&
       TakeSample Sample moduleId hitPointId then
      let adder =
        if TraceOut.Instance.IsConnected() then TraceVisit
        else AddVisit
      printfn "VisitImpl %s %d" moduleId hitPointId
      adder moduleId hitPointId context

  let private IsOpenCoverRunner() =
    (CoverageFormat = ReportFormat.OpenCoverWithTracking)
    && (TraceOut.Instance.IsDefiniteRunner()
        || (ReportFile <> "Coverage.Default.xml"
            && System.IO.File.Exists(ReportFile + ".acv")))
  let internal Granularity() = Timer
  let internal Clock() = DateTime.UtcNow.Ticks

  let internal PayloadSelection clock frequency wantPayload =
    if wantPayload() then
      match (frequency(), CallerId()) with
      | (0L, 0) -> Null
      | (t, 0) -> Time(t * (clock() / t))
      | (0L, n) -> Call n
      | (t, n) -> Both(t * (clock() / t), n)
    else Null

  let internal PayloadControl = PayloadSelection Clock
  let internal PayloadSelector enable = PayloadControl Granularity enable
  let internal lockVisits f = lock Visits f

  let internal VisitSelection track moduleId hitPointId =
    lockVisits (fun () ->
      printfn "VisitSelection %s %d" moduleId hitPointId
      VisitImpl moduleId hitPointId track)

  let Visit moduleId hitPointId =
    printfn "Visit %A %s %d" Recording moduleId hitPointId
    if Recording then
      VisitSelection (PayloadSelector IsOpenCoverRunner) moduleId hitPointId

  let internal FlushCounter (finish : Close) _ =
    lockVisits (fun () ->
      match finish with
      | Resume -> FlushResume()
      | Pause -> FlushPause()
      | _ ->
        FlushAll finish)

  // Register event handling
  let DoPause = FlushCounter Pause
  let DoResume = FlushCounter Resume

  let internal StartWatcher() =
    Watcher.Path <- Path.GetDirectoryName <| SignalFile()
    Watcher.Filter <- Path.GetFileName <| SignalFile()
    Watcher.Created.Add DoResume
    Watcher.Deleted.Add DoPause
    Watcher.EnableRaisingEvents <- Watcher.Path
                                   |> String.IsNullOrEmpty
                                   |> not

  do AppDomain.CurrentDomain.DomainUnload.Add(FlushCounter DomainUnload)
     AppDomain.CurrentDomain.ProcessExit.Add(FlushCounter ProcessExit)
     StartWatcher()