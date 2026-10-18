using System.Runtime.CompilerServices;

// Os testes EditMode do Core (Core/Tests/) testam também classes internas.
[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Core.Tests")]
[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Social")]
[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Desenvolvimento")]
[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Desenvolvimento.Editor")]
[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Desenvolvimento.Tests")]
