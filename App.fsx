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
    Free(FindElement("button", fun button ->
        match button with
        | Some button ->
            Free(FindElement("input", fun input ->
                match input with
                | Some input ->
                    printfn "%A" input
                    button.addEventListener("click", onClick (fun () -> input.Value))
                    |> Pure
                | None ->
                    Free(Log("input was not found", Pure))
            ))
        | None -> Free(Log("button was not found", Pure))
    ))

let onClick updateClock value : Program<unit> =
    let value = value()
    match parseInt value with
    | Some v ->
        Free(WriteValue((TotalSeconds,v), fun() ->
            Free(WriteValue((CurrentSecond,0), fun() ->
                setInterval updateClock 1000
                |> fun intervalKey -> Free(WriteValue((IntervalKey, intervalKey), Pure)))
            ))
        )
    | None ->
        Free(Log(sprintf "The total seconds %s was not a valid integer" value, Pure))

let onTick =
    Free(ReadValue(TotalSeconds, fun totalSeconds ->
        match totalSeconds with
        | Some total ->
            Free(ReadValue(CurrentSecond, fun currentSecond ->
                match currentSecond with
                | Some current ->
                    let updatedCurrent = current + 1
                    Free(FindElement(".clock", fun clock ->
                        match clock with
                        | Some clock ->
                            clock.textContent <- (printSeconds updatedCurrent)
                            Free(WriteValue((CurrentSecond, updatedCurrent), fun() -> 
                                if updatedCurrent = total then
                                    clock.classList.add [|"done"|]
                                    Free(ReadValue(IntervalKey, fun intervalKey ->
                                        Option.iter clearInterval intervalKey
                                        Pure()
                                    ))
                                else
                                    Pure()
                            ))
                        | None -> Free(Log("No clock found in the DOM", Pure))
                        
                    ))
                | None ->
                    Free(Log("Current second not found in store", Pure))
            ))
        | None -> Free(Log("Total seconds not found in store", Pure))
    ))

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
    onload (fun v _ -> onClick (fun () -> interpret onTick) v |> interpret)
    |> interpret

boostrap()