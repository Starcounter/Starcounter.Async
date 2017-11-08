Starcounter Async Extensions
---

h2. AsyncInputHandlers
Ordinarily Starcounter input handlers do not mix well with `await`:

```cs
public void Handle(Input.StartWorkTrigger input)
{
  this.IsBusy = true;
  await DoWorkAsync();
  // after "await" invocation we no longer have valid scheduler nor session.
  // That means we can't use DB nor update the json properties
  this.IsBusy = false; // this will not be visible to client
}
```

However, we can overcome this with the use of `AsyncInputHandlers`:

```cs
public void Handle(Input.StartWorkTrigger input)
{
  using (AsyncInputHandlers.Enable())
  {
    this.IsBusy = true;
    await DoWorkAsync(); // session is yielded, patch is calculated and sent to client
    // StarcounterSynchronizationContext restores scheduler and session after awaiting
    this.IsBusy = false; // this is executed with correct session, and results in another patch sent to client
  }
}
```