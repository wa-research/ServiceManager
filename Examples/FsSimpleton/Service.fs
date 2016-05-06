module FsSimpleton

open System.Timers
open ServiceManager

let inline notNull value = not (obj.ReferenceEquals(value, null))

type Service() = 
    let mutable _timer : Timer = null
    let secs = 9
    let interval = float secs * 1000.; 

    member this.LogEvent e =
        ServiceContext.LogInfo("Timer fired")

    member this.StartService () = 
        _timer <- new Timer(interval)
        _timer.Elapsed.Add this.LogEvent
        _timer.Start();
        ServiceContext.LogInfo (sprintf "Timer started; will log every %A seconds" secs)

    member this.StopService () =
        if notNull _timer then
            _timer.Stop()
            _timer.Dispose()