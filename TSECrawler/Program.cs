using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.IO.Compression;
using System.Linq;

namespace TSECrawler
{
    internal class Program
    {
        public const string Versao = "1.0";
        public static string diretorioLocalDados { get; set; }
        public static string urlTSE { get; set; }
        public static string IdPleito { get; set; }
        public static bool baixarApenasBu { get; set; }
        public static bool forcarDownload { get; set; }
        public static bool excluirPacoteZip { get; set; }
        public static List<string> UFs { get; set; }
        private static void ProcessarParametros(string[] args)
        {
            // Inicializar os valores padrão
            diretorioLocalDados = AppDomain.CurrentDomain.BaseDirectory;
            if (!diretorioLocalDados.EndsWith(@"\"))
                diretorioLocalDados += @"\";

            IdPleito = "406";
            urlTSE = @"https://resultados.tse.jus.br/oficial/ele2022/arquivo-urna/" + IdPleito + @"/";

            baixarApenasBu = true;

            forcarDownload = false;

            excluirPacoteZip = false;

            UFs = new List<string>();
            UFs.AddRange(new[] { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS",
                "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO", "ZZ" });

            var textoAjuda = @$"TSE Crawler Versão {Versao} - Programa para baixar arquivos do TSE.

Parametros:
    -baixartudo         Faz com que o programa baixe todos os arquivos de urna.
                        (por padrão, apenas os arquivos *.bu e *.imgbu são baixados)

    -excluirpacotezip   Exclui o pacote zip de cada seção, caso exista.

    -forcardownload     Faz com que o programa baixe novamente os arquivos que já foram baixados.
                        (por padrão, o programa baixa apenas os arquivos que não existem localmente)

    -pleito=[IdPleito]  Especifica o número do pleito. (por padrão é 406)

    -ufs=[ListaDeUFs]   Especifica quais UFs deverão ser baixadas. Lista separada por vírgulas (SP,RJ,MA).
                        (por padrão, todas as UFs são baixadas, inclusive a ""ZZ"" (Exterior))

    -saida=[Diretorio]  Especifica o diretório onde os arquivos serão salvos.
                        (por padrão, irá baixar no diretório atual).

    -ajuda, -h, -?      Exibe esta mensagem.

";

            if (args == null)
            {
                return;
            }

            foreach (var arg in args)
            {
                if (arg.ToLower().Contains("-ajuda") || arg.ToLower().Contains("-h") || arg.ToLower().Contains("-?"))
                {
                    Console.WriteLine(textoAjuda);
                    throw new Exception("Executar o programa sem nenhum argumento irá baixar todas as UFs no diretório atual.");
                }
                else if (arg.ToLower() == "-baixartudo")
                {
                    baixarApenasBu = false;
                }
                else if (arg.ToLower() == "-excluirpacotezip")
                {
                    excluirPacoteZip = true;
                }
                else if (arg.ToLower() == "-forcardownload")
                {
                    forcarDownload = true;
                }
                else if (arg.ToLower().Contains("-pleito="))
                {
                    var arr = arg.Split("=");
                    if (arr.Count() != 2)
                    {
                        Console.WriteLine(@"Argumento ""pleito"" informado incorretamente. Favor usar ""-pleito=406"", sendo neste caso 406 o número do pleito.");
                        throw new Exception("Erro ao executar o programa. Abortando.");
                    }
                    IdPleito = arr[1];
                    urlTSE = @"https://resultados.tse.jus.br/oficial/ele2022/arquivo-urna/" + IdPleito + @"/";
                }
                else if (arg.ToLower().Contains("-ufs="))
                {
                    var arr = arg.Split("=");
                    if (arr.Count() != 2)
                    {
                        Console.WriteLine(@"Argumento ""ufs"" informado incorretamente. Favor usar ""-ufs=SP,RJ,MA,BA"", informando as UFs desejadas e separando-as com vírgula.");
                        throw new Exception("Erro ao executar o programa. Abortando.");
                    }
                    var arrUFs = arr[1].Split(",");
                    UFs.Clear();
                    foreach (var uf in arrUFs)
                    {
                        UFs.Add(uf.ToUpper());
                    }
                }
                else if (arg.ToLower().StartsWith("-saida="))
                {
                    var arr = arg.Split("=");
                    if (arr.Count() != 2)
                    {
                        Console.WriteLine(@"Argumento ""saida"" inválido. Favor usar ""-saida=C:\DiretorioDeSaida"".");
                        throw new Exception("Erro ao executar o programa. Abortando.");
                    }

                    if (!Directory.Exists(arr[1]))
                    {
                        Console.WriteLine(@$"Argumento ""saida"" inválido. Diretório ""{arr[1]}"" não existe.");
                        throw new Exception("Erro ao executar o programa. Abortando.");
                    }

                    diretorioLocalDados = arr[1];
                    if (!diretorioLocalDados.EndsWith(@"\"))
                        diretorioLocalDados += @"\";
                }
            }

            var textoApresentacao = $@"TSE Crawler Versão {Versao} - Programa para baixar arquivos do TSE.

Salvando no diretório:      {diretorioLocalDados}
Baixando todos os arquivos: {(!baixarApenasBu).SimOuNao()}
Forçar download:            {forcarDownload.SimOuNao()}
Excluir Zip:                {excluirPacoteZip.SimOuNao()}
Pleito:                     {IdPleito}
UFs:                        {string.Join(",", UFs)}
";
            Console.WriteLine(textoApresentacao);
        }

        static int Main(string[] args)
        {
            try
            {
                ProcessarParametros(args);

                // Para cada estado
                foreach (var UF in UFs)
                {
                    // Criar diretório local
                    string diretorioUF = diretorioLocalDados + UF;
                    if (!Directory.Exists(diretorioUF))
                        Directory.CreateDirectory(diretorioUF);

                    // Baixar do TSE o JSON com todas as Cidades, Zonas e Seções desta UF
                    string urlConfiguracaoUF = urlTSE + @"config/" + UF.ToLower() + @"/" + UF.ToLower() + @"-p000406-cs.json";
                    string jsonConfiguracaoUF = string.Empty;

                    if (!File.Exists(diretorioUF + @"\config.json") || forcarDownload)
                    {
                        try
                        {
                            jsonConfiguracaoUF = BaixarTexto(urlConfiguracaoUF);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Erro ao baixar o arquivo de configuração da UF " + UF, ex);
                        }

                        // Salvar arquivo local para referência futura
                        File.WriteAllText(diretorioUF + @"\config.json", jsonConfiguracaoUF);
                    }
                    else
                    {
                        // O arquivo de configuração já existe. Usar ele.
                        jsonConfiguracaoUF = File.ReadAllText(diretorioUF + @"\config.json");
                    }

                    UFConfig configuracaoUF;
                    try
                    {
                        configuracaoUF = JsonSerializer.Deserialize<UFConfig>(jsonConfiguracaoUF);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro ao interpretar o JSON de configuração da UF " + UF, ex);
                    }

                    // Primeiro levantar a quantidade de seções a processar
                    int qtdSecoes = 0;
                    foreach (var abr in configuracaoUF.abr)
                        foreach (var municipio in abr.mu)
                            foreach (var zonaEleitoral in municipio.zon)
                                foreach (var secao in zonaEleitoral.sec)
                                    qtdSecoes++;

                    // Agora processar as seções
                    int secoesProcessadas = 0;

                    // Para cada ABR (seja lá o que isso signifique)
                    foreach (var abr in configuracaoUF.abr)
                    {
                        var muAtual = 0;
                        var muCont = abr.mu.Count;
                        // Para cada município
                        foreach (var municipio in abr.mu)
                        {
                            muAtual++;
                            // Criar um diretório para o município
                            string diretorioMunicipio = diretorioUF + @"\" + municipio.cd;
                            if (!Directory.Exists(diretorioMunicipio))
                                Directory.CreateDirectory(diretorioMunicipio);

                            var zeAtual = 0;
                            var zeCont = municipio.zon.Count;
                            // Para cada Zona eleitoral
                            foreach (var zonaEleitoral in municipio.zon)
                            {
                                zeAtual++;
                                // Criar um diretório para a zona eleitoral
                                string diretorioZona = diretorioMunicipio + @"\" + zonaEleitoral.cd;
                                if (!Directory.Exists(diretorioZona))
                                    Directory.CreateDirectory(diretorioZona);

                                var seAtual = 0;
                                var seCont = zonaEleitoral.sec.Count;
                                // Para cada Seçao desta zona eleitoral
                                foreach (var secao in zonaEleitoral.sec)
                                {
                                    seAtual++;
                                    secoesProcessadas++;
                                    var percentualProgresso = (secoesProcessadas.ToDecimal() / qtdSecoes.ToDecimal()) * 100;
                                    Console.WriteLine($"{percentualProgresso:N2}% - Baixando UF {UF} - Munic {municipio.cd} {municipio.nm} - Zona {zonaEleitoral.cd} - Seção {secao.ns}. M {muAtual}/{muCont}, Z {zeAtual}/{zeCont}, S {seAtual}/{seCont}.");

                                    // Criar um diretório para a seção eleitoral
                                    string diretorioSecao = diretorioZona + @"\" + secao.ns;
                                    if (!Directory.Exists(diretorioSecao))
                                        Directory.CreateDirectory(diretorioSecao);

                                    // Baixar o arquivo de configuração desta Seção
                                    string urlConfiguracaoSecao = urlTSE + @"dados/" + UF.ToLower() + @"/" + municipio.cd + @"/" + zonaEleitoral.cd + @"/" + secao.ns + @"/p000406-" + UF.ToLower() + @"-m" + municipio.cd + @"-z" + zonaEleitoral.cd + @"-s" + secao.ns + @"-aux.json";
                                    string jsonConfiguracaoSecao = string.Empty;

                                    if (!File.Exists(diretorioSecao + @"\config.json") || forcarDownload)
                                    {
                                        try
                                        {
                                            jsonConfiguracaoSecao = BaixarTexto(urlConfiguracaoSecao);
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception("Erro ao baixar o arquivo de configuração da seção " + secao.ns + ", zona " + zonaEleitoral.cd + ", município " + municipio.cd + ", UF " + UF, ex);
                                        }

                                        // Salvar arquivo local para referência futura
                                        File.WriteAllText(diretorioSecao + @"\config.json", jsonConfiguracaoSecao);
                                    }
                                    else
                                    {
                                        // O arquivo de configuração já existe. Então usar ele.
                                        jsonConfiguracaoSecao = File.ReadAllText(diretorioSecao + @"\config.json");
                                    }

                                    BoletimUrna boletimUrna;
                                    try
                                    {
                                        boletimUrna = JsonSerializer.Deserialize<BoletimUrna>(jsonConfiguracaoSecao);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Erro ao interpretar o JSON de boletim de urna da seção " + secao.ns + ", zona " + zonaEleitoral.cd + ", município " + municipio.cd + ", UF " + UF, ex);
                                    }

                                    // Agora já temos a configuração da seção. Basta baixar os arquivos
                                    foreach (var objHash in boletimUrna.hashes)
                                    {
                                        // Criar um diretório para o hash
                                        string diretorioHash = diretorioSecao + @"\" + objHash.hash;
                                        if (!Directory.Exists(diretorioHash))
                                            Directory.CreateDirectory(diretorioHash);

                                        string arquivoZip = diretorioHash + @"\pacote.zip";
                                        if (excluirPacoteZip && File.Exists(arquivoZip))
                                            File.Delete(arquivoZip);

                                        if (baixarApenasBu)
                                        {
                                            foreach (var arquivo in objHash.nmarq.FindAll(x => x.Contains(".imgbu") || x.Contains(".bu")))
                                            {
                                                string urlArquivoABaixar = urlTSE + @"dados/" + UF.ToLower() + @"/" + municipio.cd + @"/" + zonaEleitoral.cd + @"/" + secao.ns + @"/" + objHash.hash + @"/" + arquivo;
                                                var caminhoArquivo = diretorioHash + @"\" + arquivo;
                                                if ((!File.Exists(caminhoArquivo) || forcarDownload) && !string.IsNullOrWhiteSpace(arquivo))
                                                {
                                                    try
                                                    {
                                                        BaixarArquivo(urlArquivoABaixar, caminhoArquivo);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        throw new Exception("Erro ao baixar o arquivo " + arquivo + " da " + secao.ns + ", zona " + zonaEleitoral.cd + ", município " + municipio.cd + ", UF " + UF, ex);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!File.Exists(arquivoZip) || forcarDownload)
                                            {
                                                if (File.Exists(arquivoZip))
                                                    File.Delete(arquivoZip);

                                                // Para cada arquivo desse hash, baixar e salvar localmente
                                                foreach (var arquivo in objHash.nmarq)
                                                {
                                                    string urlArquivoABaixar = urlTSE + @"dados/" + UF.ToLower() + @"/" + municipio.cd + @"/" + zonaEleitoral.cd + @"/" + secao.ns + @"/" + objHash.hash + @"/" + arquivo;
                                                    var caminhoArquivo = diretorioHash + @"\" + arquivo;
                                                    if ((!File.Exists(caminhoArquivo) || forcarDownload) && !string.IsNullOrWhiteSpace(arquivo))
                                                    {
                                                        try
                                                        {
                                                            BaixarArquivo(urlArquivoABaixar, caminhoArquivo);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            throw new Exception("Erro ao baixar o arquivo " + arquivo + " da " + secao.ns + ", zona " + zonaEleitoral.cd + ", município " + municipio.cd + ", UF " + UF, ex);
                                                        }
                                                    }
                                                }

                                                // Se o Zip Temporário ainda existe, é porque o processo foi interrompido na metade. Excluir.
                                                if (File.Exists(arquivoZip + "tmp"))
                                                {
                                                    File.Delete(arquivoZip + "tmp");
                                                }

                                                // Nos interessa apenas o arquivo com extensão IMGBU, mas é bom guardar os demais arquivos.
                                                // Então vamos criar um zip com todos eles, e excluir os arquivos originais depois
                                                using (ZipArchive zip = ZipFile.Open(arquivoZip + "tmp", ZipArchiveMode.Create))
                                                {
                                                    foreach (var arquivo in objHash.nmarq)
                                                    {
                                                        if (!string.IsNullOrWhiteSpace(arquivo))
                                                        {
                                                            zip.CreateEntryFromFile(diretorioHash + @"\" + arquivo, arquivo);
                                                        }
                                                    }
                                                }

                                                // Todos os arquivos estão compactados. Excluir agora os originais (exceto o .imgbu)
                                                foreach (var arquivo in objHash.nmarq)
                                                {
                                                    if (!arquivo.ToLower().Contains(".imgbu") && !arquivo.ToLower().Contains(".bu") && !string.IsNullOrWhiteSpace(arquivo))
                                                    {
                                                        File.Delete(diretorioHash + @"\" + arquivo);
                                                    }
                                                }

                                                // Arquivos excluidos. Renomear o ZIP temporário
                                                File.Move(arquivoZip + "tmp", arquivoZip);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Processo finalizou com sucesso.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

        }

        public static void BaixarArquivo(string urlArquivo, string arquivoLocal)
        {
            int tentativas = 0;
            int maxTentativas = 10;
            using (var client = new TSEWebClient())
            {
                while (true)
                {
                    tentativas++;
                    try
                    {
                        client.DownloadFile(urlArquivo, arquivoLocal);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (tentativas > maxTentativas)
                        {
                            throw ex;
                        }

                        // Esperar 1 minuto para tentar baixar novamente
                        Console.WriteLine("Erro ao baixar o arquivo: " + ex.Message);
                        Console.WriteLine("Esperando 1 minuto para tentar novamente...");
                        Thread.Sleep(60 * 1000);
                    }

                }
            }

        }

        public static string BaixarTexto(string urlArquivo)
        {
            int tentativas = 0;
            int maxTentativas = 10;
            using (var client = new TSEWebClient())
            {
                while (true)
                {
                    tentativas++;
                    try
                    {
                        return client.DownloadString(urlArquivo);
                    }
                    catch (Exception ex)
                    {
                        if (tentativas > maxTentativas)
                        {
                            throw ex;
                        }

                        // Esperar 1 minuto para tentar baixar novamente
                        Console.WriteLine("Erro ao baixar texto do arquivo: " + ex.Message);
                        Console.WriteLine("Esperando 1 minuto para tentar novamente...");
                        Thread.Sleep(60 * 1000);
                    }

                }
            }

        }
    }


}
