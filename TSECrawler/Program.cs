using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.IO.Compression;

namespace TSECrawler
{
    internal class Program
    {
        public const string diretorioLocalDados = @"D:\Downloads\Urnas\";
        public const string urlTSE = @"https://resultados.tse.jus.br/oficial/ele2022/arquivo-urna/406/";

        static void Main(string[] args)
        {
            List<string> UFs = new List<string>();
            UFs.AddRange(new[] { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" });
            
            // Já foram baixados
            UFs.Remove("AC");
            UFs.Remove("AL");
            UFs.Remove("AP");
            UFs.Remove("AM");

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

                if (!File.Exists(diretorioUF + @"\config.json"))
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

                // Para cada ABR ?
                foreach (var abr in configuracaoUF.abr)
                {
                    foreach (var municipio in abr.mu)
                    {
                        // Criar um diretório para o município
                        string diretorioMunicipio = diretorioUF + @"\" + municipio.cd;
                        if (!Directory.Exists(diretorioMunicipio))
                            Directory.CreateDirectory(diretorioMunicipio);

                        // Para cada Zona eleitoral
                        foreach (var zonaEleitoral in municipio.zon)
                        {
                            // Criar um diretório para a zona eleitoral
                            string diretorioZona = diretorioMunicipio + @"\" + zonaEleitoral.cd;
                            if (!Directory.Exists(diretorioZona))
                                Directory.CreateDirectory(diretorioZona);

                            // Para cada Seçao desta zona eleitoral
                            foreach (var secao in zonaEleitoral.sec)
                            {
                                Console.WriteLine($"UF {UF}, Mun {municipio.cd} {municipio.nm}, Zona {zonaEleitoral.cd}, Seção {secao.ns}");
                                // Criar um diretório para a seção eleitoral
                                string diretorioSecao = diretorioZona + @"\" + secao.ns;
                                if (!Directory.Exists(diretorioSecao))
                                    Directory.CreateDirectory(diretorioSecao);

                                // Baixar o arquivo de configuração desta Seção
                                string urlConfiguracaoSecao = urlTSE + @"dados/" + UF.ToLower() + @"/" + municipio.cd + @"/" + zonaEleitoral.cd + @"/" + secao.ns + @"/p000406-" + UF.ToLower() + @"-m" + municipio.cd + @"-z" + zonaEleitoral.cd + @"-s" + secao.ns + @"-aux.json";
                                string jsonConfiguracaoSecao = string.Empty;

                                if (!File.Exists(diretorioSecao + @"\config.json"))
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
                                    if (!File.Exists(arquivoZip))
                                    {
                                        // Para cada arquivo desse hash, baixar e salvar localmente
                                        foreach (var arquivo in objHash.nmarq)
                                        {
                                            string urlArquivoABaixar = urlTSE + @"dados/" + UF.ToLower() + @"/" + municipio.cd + @"/" + zonaEleitoral.cd + @"/" + secao.ns + @"/" + objHash.hash + @"/" + arquivo;
                                            if (!File.Exists(diretorioHash + @"\" + arquivo) && !string.IsNullOrWhiteSpace(arquivo))
                                            {
                                                try
                                                {
                                                    BaixarArquivo(urlArquivoABaixar, diretorioHash + @"\" + arquivo);
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
                                            if (!arquivo.ToLower().Contains(".imgbu"))
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

            Console.WriteLine("Processo finalizou com sucesso.");
        }

        public static void BaixarArquivo(string urlArquivo, string arquivoLocal)
        {
            int tentativas = 0;
            int maxTentativas = 5;
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
                        if (!ex.Message.Contains("Slow Down") && !ex.Message.Contains("timed out"))
                        {
                            throw ex;
                        }

                        if (tentativas > maxTentativas)
                        {
                            throw ex;
                        }

                        // Esperar 1 minuto para tentar baixar novamente
                        Console.WriteLine("Erro ao baixar o arquivo. Esperando 1 minuto para tentar novamente...");
                        Thread.Sleep(60 * 1000);
                    }

                }
            }

        }

        public static string BaixarTexto(string urlArquivo)
        {
            int tentativas = 0;
            int maxTentativas = 5;
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
                        if (!ex.Message.Contains("Slow Down") && !ex.Message.Contains("timed out"))
                        {
                            throw ex;
                        }

                        if (tentativas > maxTentativas)
                        {
                            throw ex;
                        }

                        // Esperar 1 minuto para tentar baixar novamente
                        Console.WriteLine("Erro ao baixar texto. Esperando 1 minuto para tentar novamente...");
                        Thread.Sleep(60 * 1000);
                    }

                }
            }

        }
    }

    public class UFConfig
    {
        public string dg { get; set; } // Data da apuração ? "02/10/2022"
        public string hg { get; set; } // Hora da apuração ? "19:27:49"
        public string f { get; set; } // Não sei ? "0"
        public string cdp { get; set; } // Não sei ? "406"
        public List<ABR> abr { get; set; } // Não sei o que significa ABR, mas a lista de municípios está dentro
    }

    public class ABR
    {
        public string cd { get; set; } // Código da UF "AP"
        public string ds { get; set; } // Descrição da UF "AMAPÁ"
        public List<Municipio> mu { get; set; } // Lista de municípios
    }

    public class Municipio
    {
        public string cd { get; set; } // Código do município "06050"
        public string nm { get; set; } // Nome do Município
        public List<ZonaEleitoral> zon { get; set; } // Zonas eleitorais do Município

    }

    public class ZonaEleitoral
    {
        public string cd { get; set; } // Código da zona eleitoral "0002"
        public List<SecaoEleitoral> sec { get; set; } // Seção eleitoral
    }

    public class SecaoEleitoral
    {
        public string ns { get; set; } // Número da Seção eleitoral "0001"
        public string nsp { get; set; } // Número da Seção eleitoral "0001" (repetido porque?)
    }

    public class BoletimUrna
    {
        public string dg { get; set; } // Data da apuração? "02/10/2022"
        public string hg { get; set; } // Hora da apuração? "23:27:25"
        public string f { get; set; } // Não sei? "0"
        public string st { get; set; } // Situação? "Totalizada"
        public string ds { get; set; } // Não sei? ""
        public List<BoletimUrnaHash> hashes { get; set; } // Lista de Hashes
    }

    public class BoletimUrnaHash
    {
        public string hash { get; set; } // Hash "534f753676357056516e4a42384e376c77544b32533257562b794a2d4968375a6e7a654f504b6746762b493d"
        public string dr { get; set; } // Data do hash? "02/10/2022"
        public string hg { get; set; } // Hora do hash? "19:20:08"
        public string st { get; set; } // Situação? "Totalizado"
        public string ds { get; set; } // Não sei? ""
        public List<string> nmarq { get; set; } // Lista dos nomes dos arquivos
    }

    public class TSEWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            int itimeout = Convert.ToInt32(TimeSpan.FromSeconds(60).TotalMilliseconds);
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = itimeout;
            ((HttpWebRequest)w).ReadWriteTimeout = itimeout;
            return w;
        }
    }
}
