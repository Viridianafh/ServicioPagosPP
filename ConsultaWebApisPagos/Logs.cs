using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaWebApisPagos
{
    public class Logs
    {
        public static void crealogs(string action)
        {
            DateTime now = DateTime.Now;
            string fecsnhr=now.ToString("dd-MM-yyyy");
            string nombreArchivo = "Log_Valida_Pago"+fecsnhr+".txt";
            string ruta = "C:/Logs_validador_pagos/" + nombreArchivo;

            using (FileStream fs = new FileStream(ruta, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine(action);
                }
            }
        }
    }
}
