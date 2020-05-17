﻿// # namespace `AltCover`
// ```
namespace AltCover

open System.Runtime.CompilerServices

// ```
// ## module `PrepareExtension` and module `CollectExtension`
// ```
[<Extension>]
module PrepareExtension = begin
  [<Extension>]
  val WhatIf : prepare:Abstract.IPrepareOptions -> AltCover.ValidatedCommandLine
end
[<Extension>]
module CollectExtension = begin
  [<Extension>]
  val WhatIf :
    collect:Abstract.ICollectOptions ->
      afterPreparation:bool -> AltCover.ValidatedCommandLine
end
// ```
// These provide C#-compatible extension methods to perform a `WhatIf` style command line validation
//
// `WhatIf` compiles the effective command-line and the result of `Validate`