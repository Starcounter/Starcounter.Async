Starcounter Async Extensions
---

To introduce asynchronicity in Starcounter TypedJSON view-model, you have to use callbacks and handle errors carefully:

```cs
public void Handle(Input.StartWorkTrigger input)
{
    this.IsBusy = true;
    Task.Run(LengthyJob)
        .ContinueWith(task => Session.ScheduleTask(Session.Current.SessionId,
        (Session session, string sessionId) => {
            // might happen if this code is executed after the session has been destroyed
            if (session == null)
            {
                return;
            }
            try
            {
                // if LengthyJob resulted in exception, it will be unwrapped here
                this.Result = task.Result;
            }
            catch (Exception e)
            {
                this.Result = "Error";
            }
            finally
            {
                this.IsBusy = false;
                // otherwise the changes won't be immediately visible to the client
                session.CalculatePatchAndPushOnWebSocket();
            }
        }));
}
```

This library allows you to simplify this code by using async-await

```cs
public void Handle(Input.StartWorkTrigger input)
{
    AsyncInputHandlers.Run(async () =>
    {
        this.IsBusy = true;
        try
        {
            this.Result = await LengthyJob();
        }
        catch(Exception e)
        {
            this.Result = "Error";
        }
        finally
        {
            this.IsBusy = false;
        }
    });
}
```