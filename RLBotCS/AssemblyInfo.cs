using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("3d067f37-99cf-4f21-be38-c5a10dc240d3")]

// https://improveandrepeat.com/2019/12/how-to-test-your-internal-classes-in-c/
[assembly: InternalsVisibleTo("RLBotCSTests")]
