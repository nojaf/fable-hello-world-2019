#load ".paket/load/main.group.fsx"

open Fable.Core
open Fable.Core.JS
open Browser.Dom
open Browser.Types

type Element with
    [<Emit("$0.value")>]
    member this.Value : string = jsNative

// Using the Free monad
type Instruction<'a> =
    | ReadValue of string * (int option -> 'a)
    | WriteValue of (string * int) * (unit -> 'a)
    | FindElement of string * (Element option -> 'a)
    | Log of string * (unit -> 'a)

type Program<'a> =
    | Free of Instruction<Program<'a>>
    | Pure of 'a

let private mapI f instr =
    match instr with
    | ReadValue(identifier, next) -> ReadValue(identifier, next >> f)
    | WriteValue((identifier, value), next) -> WriteValue((identifier, value), next >> f)
    | FindElement(selector, next) -> FindElement(selector, next >> f)
    | Log(entry, next) -> Log(entry, next >> f)

let rec bindI<'a> (fn : 'a -> Program<'b>) (program : Program<'a>) : Program<'b> =
    match program with
    | Free a -> 
        a
        |> mapI (bindI fn)
        |> Free
    | Pure a -> fn a
    
type FreeBuilder() =
    member this.Bind(x, f) = bindI f x
    member this.Return x = Pure x
    member this.ReturnFrom x = x
    member this.Zero() = Pure()

let readValue key = Free(ReadValue(key, Pure))
let writeValue key value = Free(WriteValue((key,value), Pure))
let log entry = Free(Log(entry, Pure))
let findElement selector = Free(FindElement(selector, Pure))

let free = FreeBuilder()

let [<Literal>] CurrentSecond = "CurrentSecond"
let [<Literal>] TotalSeconds = "TotalSeconds"
let [<Literal>] IntervalKey = "IntervalKey"

let private parseInt (value:string) =
    match System.Int32.TryParse(value) with
    | true, v -> Some v
    | _ -> None

let private printSeconds secondes =
    let minutes = secondes / 60
    let remainingSeconds = secondes % 60
    sprintf "%02d:%02d" minutes remainingSeconds

let onload onClick =
    free {
        let! button = findElement "button"
        let! input = findElement "input"
        let attachHandler =
            match button, input with
            | Some button, Some input ->
                button.addEventListener("click", onClick (fun () -> input.Value))
                Pure()
            | _ ->
                log "input or button were not found."
        do! attachHandler
    }

let onClick updateClock value : Program<unit> =
    let value = value()
    match parseInt value with
    | Some v ->
        free {
            do! writeValue TotalSeconds v
            do! writeValue CurrentSecond 0
            let intervalKey = setInterval updateClock 1000
            do! writeValue IntervalKey intervalKey
        }
    | None ->
        log (sprintf "The total seconds %s was not a valid integer" value)

let onTick =
    let addSecond total current =
        free {
            let updatedCurrent = current + 1
            let! clock = findElement ".clock"

            match clock with
            | Some clock ->
                clock.textContent <- (printSeconds updatedCurrent)
                do! writeValue CurrentSecond updatedCurrent

                if updatedCurrent = total then
                    clock.classList.add [|"done"|]
                    let! intervalKey = readValue IntervalKey
                    Option.iter clearInterval intervalKey

            | None ->
                do! log "Clock not found"
        }

    free {
        let! totalSeconds = readValue TotalSeconds
        let! currentSecond = readValue CurrentSecond
        let processTick =
            match totalSeconds, currentSecond with
            | Some total, Some current -> addSecond total current
            | _ -> log "current second or total second not found"

        do! processTick
    }

let rec interpret programInstr =
    match programInstr with
    | Pure x -> x
    | Free(ReadValue(key, next)) ->
        window.localStorage.getItem key
        |> parseInt
        |> next
        |> interpret
    | Free(WriteValue((key, value), next)) ->
        window.localStorage.setItem(key, sprintf "%d" value)
        |> next
        |> interpret
    | Free(FindElement(selector, next)) ->
        document.querySelector selector
        |> Option.ofObj
        |> next
        |> interpret
    | Free(Log(entry, next)) ->
        printfn "%s" entry
        |> next
        |> interpret

let boostrap () =
    let updateClock() = interpret onTick
    onload (fun v _ -> onClick updateClock v |> interpret)
    |> interpret

boostrap()