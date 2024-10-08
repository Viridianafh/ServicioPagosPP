using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaWebApisPagos
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Version 7");
            DateTime hora_actual = DateTime.Now;
            DateTime time_30, time_60;
            time_30 = hora_actual.AddMinutes(-30);
            time_60 = hora_actual.AddMinutes(-60);
            string dia_actual = Convert.ToInt32(time_30.Day) < 10 ? "0" + (time_30.Day).ToString() : (time_30.Day).ToString();
            string dia_actual2 = Convert.ToInt32(time_60.Day) < 10 ? "0" + (time_60.Day).ToString() : (time_60.Day).ToString();
            string dia_actual3 = Convert.ToInt32(hora_actual.Day) < 10 ? "0" + (hora_actual.Day).ToString() : (hora_actual.Day).ToString();
            string starting_day = time_30.Year + "-" + time_30.Month + "-" + dia_actual + " " + time_30.TimeOfDay.Hours + ":" + time_30.TimeOfDay.Minutes + ":00";
            string ending_day = time_60.Year + "-" + time_60.Month + "-" + dia_actual2 + " " + time_60.TimeOfDay.Hours + ":" + time_60.TimeOfDay.Minutes + ":00";
            string day = hora_actual.Year + "-" + hora_actual.Month + "-" + dia_actual + " " + hora_actual.TimeOfDay.Hours + ":" + hora_actual.TimeOfDay.Minutes + ":00";
            //starting_day = "2022-12-27 20:00:00";
            //ending_day = "2022-12-27 20:00:00";
            //day = "2022-12-27 23:30:00";
            //Metodos.GetTipoCambio();
            //Metodos.SetPedidos();
            //Metodos.setPedidosOpenPay();
            //Console.WriteLine("Terminan Metodos");
            //Console.ReadKey();

            /*INICIO PRODUCTIVO*/
            Logs.crealogs("\n---------------------------------------------------------------INICIO EJECUCION---------------------------------------------------------------");
           
            Metodos.BuscarPagosPayPal(day, starting_day, "0");
            //Metodos.BuscarPagosPayPal(starting_day, ending_day, "1");
            
            Logs.crealogs("\n---------------------------------------------------------------FINAL EJECUCION---------------------------------------------------------------");
        }

    }
}
