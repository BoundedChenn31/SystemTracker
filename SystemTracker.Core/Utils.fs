namespace SystemTracker.Core.Utils

module ExceptionTransformers =

    let inline tryToOption (action: 'T -> 'U) (input: 'T): Option<'U> =
        try action input |> Some
        with e -> None

    let inline tryToResult (action: 'T -> 'U) (input: 'T): Result<'U, exn> =
        try action input |> Ok
        with e -> Error e
