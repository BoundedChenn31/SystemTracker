open System
open NodaTime
open SystemTracker.Core
open SystemTracker.Core.Rules
open SystemTracker.Core.SystemScanner
open SystemTracker.Core.Utils

type Console with
    static member IgnoreLine() = Console.ReadLine() |> ignore

type Instant with
    member this.InSystemDefault (zoneProvider: IDateTimeZoneProvider): ZonedDateTime =
        zoneProvider.GetSystemDefault() |> this.InZone

type MyRule(likes: string[]) =
    interface IRule with
        member x.IsCompliant ps =
            likes
            |> Array.tryFind(fun pn ->
                ps.ExecutableNameWithoutExtension.Contains(pn, StringComparison.InvariantCultureIgnoreCase))
            |> Option.isNone

[<EntryPoint>]
let main _ =
    let names = [| "msedge"; "telegram" |]
    let worthPrinting (ps: ProcessSnapshot) =
        names
        |> Array.exists (fun n ->
            n.Contains(ps.ExecutableNameWithoutExtension,
                       StringComparison.InvariantCultureIgnoreCase))

    let prepareConsole () =
        Console.Clear()
        Console.WriteLine "Press <Enter> to stop watchdog..."

    let tzdb = DateTimeZoneProviders.Tzdb

    let configuration = { SnapshotsInterval = Duration.FromSeconds 1L }

    use watchdog = new Watchdog(configuration, SystemClock.Instance)

    watchdog.SnapshotFormed.Add(fun x ->
        prepareConsole()
        Console.WriteLine($"Local Time: {x.Timestamp.InSystemDefault(tzdb).LocalDateTime}")
        x.ProcessSnapshots
        |> Seq.where worthPrinting
        |> Seq.sortBy (fun ps -> ps.Pid)
        |> Seq.iter (fun ps ->
            Console.WriteLine $"{ps.Pid, 6} | {ps.ExecutableNameWithoutExtension}")
        )

    watchdog.Start()
    prepareConsole()
    Console.IgnoreLine()
    watchdog.Stop()
    0
