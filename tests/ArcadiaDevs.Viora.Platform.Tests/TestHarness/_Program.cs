// Marker class that exposes the top-level Program entry point of the
// ArcadiaDevs.Viora.Platform host to WebApplicationFactory<Program> in
// the test project. Without this partial class declaration the test
// project cannot reference Program (it is implicit because Program.cs
// uses top-level statements). See
// tests/ArcadiaDevs.Viora.Platform.Tests/README.md for the harness usage.
public partial class Program { }
