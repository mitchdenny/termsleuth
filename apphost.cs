#:package Aspire.Hosting.Dotnet@17.0.0-preview.1.26512.6
#:sdk Aspire.AppHost.Sdk@17.0.0-preview.1.26512.6
#:property AspireUseCliBundle=true
#:property NoWarn=ASPIRETERMINAL001

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("wordsleuth", "./src/WordSleuth/WordSleuth.csproj")
    .WithTerminal(options =>
    {
        options.Columns = 80;
        options.Rows = 60;
    });

builder.Build().Run();