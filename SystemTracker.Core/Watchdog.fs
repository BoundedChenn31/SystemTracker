namespace SystemTracker.Core

open System
open System.Threading
open System.Timers
open NodaTime
open SystemTracker.Core.Rules
open SystemTracker.Core.SystemScanner

type WatchdogConfiguration =
    { SnapshotsInterval: Duration }

type SnapshotFormedEventArgs =
    { ProcessSnapshots: seq<ProcessSnapshot>
      Timestamp: Instant }

type Watchdog(configuration: WatchdogConfiguration, clock: IClock) =

    let timer: Timer = new Timer()

    let mutable timerSyncPoint = 0
    let [<Literal>] SyncPoint_Available = 0
    let [<Literal>] SyncPoint_Taken = 1
    let [<Literal>] SyncPoint_TimerStopped = -1

    let snapshotFormedEvent = Event<EventHandler<SnapshotFormedEventArgs>, SnapshotFormedEventArgs>()

    let debug = false // TODO: remove

    [<CLIEvent>] member _.SnapshotFormed = snapshotFormedEvent.Publish

    member private this.HandleElapsedInterval(_: ElapsedEventArgs) =
        this.DebugReport "Sleeping at the start of handler for some reason"
        this.DebugSleep 1000
        match Interlocked.CompareExchange(&timerSyncPoint, SyncPoint_Taken, SyncPoint_Available) with
        | SyncPoint_Available ->
            this.DebugReport "Gathering data for a long time"
            this.DebugSleep 3000
            let snapshots = SystemScanner.getProcesses(clock)
            snapshotFormedEvent.Trigger(this, { ProcessSnapshots = snapshots
                                                Timestamp = clock.GetCurrentInstant() })
            this.DebugReport "Finished handling, releasing sync point."
            timerSyncPoint <- SyncPoint_Available
        | SyncPoint_Taken -> // previous handler has not finished yet
            this.DebugReport "Warning! Data gathering takes longer than timer interval, skipping..."
        | SyncPoint_TimerStopped -> // timer has been stopped
            this.DebugReport "Timer was stopped, skipping..."
        | _ -> // TODO: this should not happen. Right?
            failwith $"""{DateTime.Now.ToString "hh:mm:ss.fff"} - This shouldn't happen. Right?"""

    member private this.DebugSleep ms =
        if debug then Thread.Sleep ms

    member private this.DebugReport msg =
        if debug then
            sprintf "%d - %s - %s"
                Thread.CurrentThread.ManagedThreadId
                (DateTime.Now.ToString "hh:mm:ss.fff")
                msg
            |> Console.WriteLine

    member this.Start() =
        timer.Elapsed.Add this.HandleElapsedInterval
        timer.Interval <- configuration.SnapshotsInterval.TotalMilliseconds
        timer.Start()

    member this.Stop() =
        timer.Stop()
        while Interlocked.CompareExchange(&timerSyncPoint,
                                          SyncPoint_TimerStopped,
                                          SyncPoint_Available) <> SyncPoint_Available do
            Thread.Sleep 1

    interface IDisposable with
        member this.Dispose() = timer.Dispose()

    member this.Dispose() = (this :> IDisposable).Dispose()
