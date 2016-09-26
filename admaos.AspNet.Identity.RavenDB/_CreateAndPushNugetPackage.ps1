Remove-Item *.nupkg
nuget pack admaos.AspNet.Identity.RavenDB.csproj -Build -Prop Configuration=Release -Symbols
nuget setApiKey (Read-Host 'Nuget API-Key') -Source https://www.nuget.org/api/v2/package
nuget push (Get-ChildItem *.nupkg -Exclude *symbols*) -Source https://www.nuget.org/api/v2/package