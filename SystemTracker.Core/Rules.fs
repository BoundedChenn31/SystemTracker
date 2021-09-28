namespace SystemTracker.Core.Rules

open System
open FSharpx.Collections
open SystemTracker.Core.SystemScanner
open SystemTracker.Core.Utils

type IRule = interface
    abstract member IsCompliant: processSnapshot: ProcessSnapshot -> bool
end

