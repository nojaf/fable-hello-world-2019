#load ".paket/load/main.group.fsx"

open Fable.Core
open Fable.Core.JS
open Browser.Dom
open Browser.Types

let [<Literal>] CurrentSecond = "CurrentSecond"
let [<Literal>] TotalSeconds = "TotalSeconds"
let [<Literal>] IntervalKey = "IntervalKey"

[<Emit("document.querySelector($0)")>]
let bySelector<'t> selector : 't option = jsNative

let private parseInt (value:string) =
    match System.Int32.TryParse(value) with
    | true, v -> Some v
    | _ -> None

let private getFromLocalStorage key =
    window.localStorage.getItem key
    |> parseInt

let private putInLocalStorage key value =
    window.localStorage.setItem(key, sprintf "%d" value)

let private getClock() = bySelector<HTMLDivElement> ".clock"

let private printSeconds secondes =
    let minutes = secondes / 60
    let remainingSeconds = secondes % 60
    sprintf "%02d:%02d" minutes remainingSeconds

let private updateClock () =
    match getFromLocalStorage TotalSeconds, getFromLocalStorage CurrentSecond with
    | Some total, Some current ->
        let updatedCurrent = current + 1
                
        updatedCurrent
        |> putInLocalStorage CurrentSecond

        getClock()
        |> Option.iter(fun clock -> clock.textContent <- (printSeconds updatedCurrent))

        if total = updatedCurrent then
            getClock()
            |> Option.iter(fun clock -> clock.classList.add [|"done"|])
            getFromLocalStorage IntervalKey
            |> Option.iter(clearInterval)

    | _ ->
        printfn "The total and/or the current second could not be found in the localStorage"

match bySelector<HTMLButtonElement> "button", bySelector<HTMLInputElement> "input" with
| Some button, Some input ->
    button.addEventListener("click", (fun _ ->
        parseInt input.value
        |> Option.iter (fun v ->
            putInLocalStorage TotalSeconds v
            putInLocalStorage CurrentSecond 0
            setInterval updateClock 1000
            |> putInLocalStorage IntervalKey
            getClock()
            |> Option.iter(fun clock -> clock.classList.remove [|"done"|])
        )
    ))
| _ ->
    printfn "The button and/or the input could not be found in the DOM."