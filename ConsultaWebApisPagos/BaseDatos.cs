using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaWebApisPagos
{
    public class BaseDatos
    {
        public static DataTable MandaQuerySelect(string sqry)
        {
            var cadenaConexion = "server = medrano-prod.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=sBMH8fvnPM;";
            //string cadenaConexion = "server = pruebasdev.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=medranodev24;Include Error Detail=true;";
            NpgsqlConnection conexion = new NpgsqlConnection(cadenaConexion);
            NpgsqlCommand comando;
            NpgsqlDataAdapter adaptador;
            DataTable tbl = new DataTable();

            string Result = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(cadenaConexion))
                {

                    comando = new NpgsqlCommand(sqry, conexion);
                    comando.CommandType = CommandType.Text;
                    conexion.Open();
                    comando.ExecuteNonQuery();//Revisar, se ejecuta de nuevo el query
                    conexion.Close();
                    tbl = new DataTable();
                    adaptador = new NpgsqlDataAdapter(comando);
                    adaptador.Fill(tbl);
                    Result = "Correcto";
                    return tbl;
                }
                return tbl;
            }
            catch (Exception ex)
            {
                Logs.crealogs(DateTime.Now.ToString() + " Error ejecucion query: " + sqry + "Devolvio: " + ex.Message);
                return null;
            }                       
        }
        public static string MandaQueryInsertDeleteUpdate(string sqry)
        {
            var cadenaConexion = "server = medrano-prod.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=sBMH8fvnPM;";
            //string cadenaConexion = "server = pruebasdev.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=medranodev24;Include Error Detail=true;";
            NpgsqlConnection conexion = new NpgsqlConnection(cadenaConexion);
            NpgsqlCommand comando;
            NpgsqlDataAdapter adaptador;
            DataTable tbl = new DataTable();

            string Result = string.Empty;

            if (!string.IsNullOrWhiteSpace(cadenaConexion))
            {
                try
                {
                    comando = new NpgsqlCommand(sqry, conexion);
                    comando.CommandType = CommandType.Text;
                    conexion.Open();
                    comando.ExecuteNonQuery();//Revisar, se ejecuta de nuevo el query
                    conexion.Close();
                    Result = "Correcto";
                    return Result;
                }
                catch (Exception ex)
                {
                    Logs.crealogs(DateTime.Now.ToString() + " Error ejecucion query: " + sqry + "Devolvio: " + ex.Message);
                    return "Error"+ex.Message;
                }                
            }
            return Result;
        }
    }
}
