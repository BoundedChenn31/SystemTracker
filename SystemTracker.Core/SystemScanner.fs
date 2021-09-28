namespace SystemTracker.Core.SystemScanner

open NodaTime
open System.Diagnostics
open SystemTracker.Core.Utils

type ProcessSnapshot =
    { Pid: int
      ExecutableNameWithoutExtension: string
      FinishTime: int64 } // Set as a final step after gathering all information about process

[<RequireQualifiedAccess>]
module SystemScanner =

    open ExceptionTransformers

    let getProcesses (clock: IClock) =
        let processes = Process.GetProcesses()
        processes |> Seq.choose (fun p ->
            use p = p
            p
            |> tryToOption (fun p ->
                { Pid = p.Id
                  ExecutableNameWithoutExtension = p.ProcessName
                  FinishTime = clock.GetCurrentInstant().ToUnixTimeMilliseconds() }
                )
            )
