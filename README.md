# admaos.AspNet.Identity.RavenDB

A fully async RavenDB implementation of AspNet.Identity

## Installation instructions

1. Create a new ASP.NET MVC project and select the Individual User Accounts authentication type.

2. Remove the Entity Framework packages and install the admaos.AspNet.Identity.RavenDB package:
    ```
    Uninstall-Package Microsoft.AspNet.Identity.EntityFramework
    Uninstall-Package EntityFramework
    Install-Package admaos.AspNet.Identity.RavenDB
    ```

3. In ~/Models/IdentityModels.cs:
    - Remove the namespace: ```System.Data.Entity```
    - Remove the namespace: ```Microsoft.AspNet.Identity.EntityFramework```
    - Add the namespace: ```admaos.AspNet.Identity.RavenDB```
    - Replace the class ApplicationDbContext with 
    ```
    public class ApplicationDocumentSession : AsyncDocumentSession
        {

            private ApplicationDocumentSession(string dbName, DocumentStore documentStore, IAsyncDatabaseCommands asyncDatabaseCommandsExtensions, DocumentSessionListeners listeners, Guid id) : base(dbName, documentStore, asyncDatabaseCommandsExtensions, listeners, id)
            {

            }

            public static IAsyncDocumentSession Create()
            {
                //Below code is just to showcase the functionality, please don't use this in production (e.g. use dependency injection instead)
                var st = new DocumentStore { Url = "http://localhost:8080" };
                st.RegisterListener(new UniqueConstraintsStoreListener());
                st.DefaultDatabase = "YourDatabase";
                st.Initialize();
                return st.OpenAsyncSession();
            }
        }
    ```

4. In ~/App-Start/IdentityConfig.cs:
    - Remove the namespace: ```System.Data.Entity```
    - Remove the namespace: ```Microsoft.AspNet.Identity.EntityFramework```
    - Add the namespace: ```admaos.AspNet.Identity.RavenDB```
    - Replace ```ApplicationDbContext``` with ```IAsyncDocumentSession```

5. In ~/App_Start/Startup.Auth.cs:
    - Replace ```ApplicationDbContext``` with ```ApplicationDocumentSession```