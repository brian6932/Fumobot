﻿using Autofac;
using Fumo.Extensions.AutoFacInstallers;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Fumo;

internal class Program
{
    static void Main(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        var configPath = args.Length > 0 ? args[0] : "config.json";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(cwd)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        var builder = new ContainerBuilder();

        builder
            .InstallConfig(configuration)
            .InstallSerilog(configuration)
            .InstallDatabase(configuration);

        var container = builder.Build();

        Log.Logger.Information("xd");

        //var builder = new ContainerBuilder();
        //builder.RegisterType<Idiot>().InstancePerDependency();
        //builder.RegisterType<Dependency>().InstancePerDependency();
        //var container = builder.Build();

        //////using (var scope = container.BeginLifetimeScope(b => b.RegisterType<Idiot>().SingleInstance()))
        //////{
        //////    var idiot = scope.Resolve<Idiot>();
        //////    idiot.Foo();
        //////}

        ////SomeData Data = new("Foo");

        ////// register Data in the lifetime scope and resolve in Idiot
        ////using (var scope = container.BeginLifetimeScope(b => b.RegisterInstance(Data)))
        ////{
        ////    var idiot = scope.Resolve<Idiot>();
        ////    idiot.Foo();
        ////}

        //Activator.CreateInstance(

        //    );

        //Console.ReadLine();
    }
}

//public interface IIdiot
//{
//    void Foo();
//}

//public record SomeData(string Foo);

//public class Idiot : IIdiot
//{
//    public required Dependency Dependency { protected get; init; }

//    public SomeData Data { get; }

//    public Idiot(SomeData data)
//    {
//        Data = data;
//    }

//    public void Foo()
//    {
//        Console.WriteLine(this.Data.Foo);

//        this.Dependency.Yes();
//    }
//}

//public class Dependency
//{
//    public void Yes()
//    {
//        Console.WriteLine("Yes");
//    }
//}