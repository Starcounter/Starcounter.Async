This is a showcase of async Input Handlers
---

1. Build and run
1. Open http://localhost:8080/modal and http://localhost:8080/background


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

However, we can overcome this with the use of `StarcounterSynchronizationContext`:

```cs
public void Handle(Input.StartWorkTrigger input)
{
  using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
  {
    this.IsBusy = true;
    await DoWorkAsync(); // session is yielded, patch is calculated and sent to client
    // StarcounterSynchronizationContext restores scheduler and session after awaiting
    this.IsBusy = false; // this is executed with correct session, and results in another patch sent to client
  }
}
```
