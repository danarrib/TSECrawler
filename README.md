# TSE Crawler

Um programa feito para baixar os arquivos de boletins de urna de todas as seções eleitorais do Brasil (e do exterior). Foi feito com foco nas eleições de 2022.

Atenção: Este programa não processa arquivos, ele apenas faz o download. Para processar os boletins de urna, você pode se interessar pelo [TSE Parser](https://github.com/danarrib/TSEParser).

O TSE Crawler funciona da seguinte forma:

- Especificar o diretório onde deseja salvar os arquivos na constante `diretorioLocalDados`. (por padrão, é `D:\Downloads\Urnas\`)

- Especificar se deseja baixar apenas os boletins de urna, ou todos os arquivos da urna definido o valor da constante `baixarApenasBu` 

- O programa tem uma lista de UFs (Unidades da Federação, estados como SP, RJ, PR, etc). Para cada UF, o programa irá:
  
  - Baixar o JSON de configuração desta UF. Este JSON contém todos os Municípios, todas as Zonas Eleitorais e todas as Seções Eleitorais. 
  
  - Cada Seção Eleitoral é uma Urna.
  
  - Para cada Seção Eleitoral, de cada Zona Eleitoral, de cada Município:
    
    - Baixar o JSON de configuração da Seção, que contém a lista de arquivos da Urna, e o Hash, que é uma string que é necessária para montar a URL de download dos arquivos.
    
    - Para cada arquivo descrito na lista de arquivos, o sistema baixa e salva no disco, utilizando a estrutura de diretório `diretorioLocalDados\{UF}\{CodMunicipio}\{CodZonaEleitoral}\{CodSecaoEleitoral}\{Hash}\{nomeArquivo}`.

O sistema propositalmente não utiliza processamento paralelo para o download, pois os servidores do TSE detectam excesso de requisições e começam a enviar erro HTTP 503 (Slow Down). 

Mesmo fazendo um único download de cada vez, eventualmente este erro ocorre. O programa está preparado para lidar com isso, esperando 1 minuto antes de tentar baixar o arquivo novamente. Normalmente o problema se resolve sozinho com apenas uma nova tentativa de download, mas o programa está feito de modo a tentar 10 vezes antes de declarar uma falha fatal.

Para mim, baixar todos os arquivos `*.bu[sa]` e `*.imgbu[sa]` levou cerca de 80 horas, e consumiu 50GB de espaço em disco.

O programa foi escrito em C# com [.NET Core 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1) usando o [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/pt-br/vs/community/).
