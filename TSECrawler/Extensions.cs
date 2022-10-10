using System;
using System.Collections.Generic;
using System.Text;

namespace TSECrawler
{
    public static class Extensions
    {
        public static decimal ToDecimal(this int value)
        {
            return Convert.ToDecimal(value);
        }
        public static string SimOuNao(this bool value)
        {
            return value ? "Sim" : "Não";
        }
    }

}
