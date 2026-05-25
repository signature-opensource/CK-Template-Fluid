using CK.Core;
using CK.Template.Fluid;

namespace CK.Template.Fluid.Tests.Engine.Fixtures;

/// <summary>Bound to the <c>Greeting.*.liquid</c> templates under Res/Templates/.</summary>
[FluidTemplate( "Greeting" )]
public interface IGreetingModel : IPoco
{
    string Name { get; set; }
}
