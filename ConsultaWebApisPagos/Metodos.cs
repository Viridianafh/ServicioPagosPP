using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace ConsultaWebApisPagos
{
    class Metodos
    {
        
        #region DatosBoleto       
        #endregion DatosBoleto
        #region MasterCard
        public static bool BuscarPagosMasterCard(string starting_day, string ending_day, string op)
        {
            Console.WriteLine("################----MasterCard----#######################");
            Logs.crealogs("\n" + DateTime.Now.ToString() + " ################----MasterCard----#######################");
            //2022-12-06 23:24:15.2540000
            string concepto = "";
            string correo_envio = "", short_id = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime hora_actual = DateTime.Now;
            hora_actual = hora_actual.AddMinutes(-30);
            DateTime time = DateTime.Now;
            //Console.WriteLine(starting_day); 
            string dia_actual = Convert.ToInt32(hora_actual.Day) < 10 ? "0" + (hora_actual.Day).ToString() : (hora_actual.Day).ToString();
            string hora_inicio = hora_actual.Year + "-" + hora_actual.Month + "-" + dia_actual + " " + hora_actual.TimeOfDay.Hours + ":" + hora_actual.TimeOfDay.Minutes + ":00";
            bool verificacion = false;
            DataTable tbl = new DataTable();
            bool Result = true;
            Console.WriteLine("Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            Logs.crealogs(DateTime.Now.ToString() + " Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            DateTime actual = DateTime.Now.AddHours(-2);
            string formattedDateTime = actual.ToString("yyyy-MM-dd HH:mm:ss.fffzzz");
            string sqry = @"SELECT * 
                            FROM internet_sale 
                            WHERE payment_provider LIKE '%aster%ard%' 
                            AND NOT payment_provider LIKE '%Gateway%' 
                            --AND payed = False 
                            AND ((source_meta NOT LIKE 'Validado y envio de correo' 
                            AND source_meta NOT LIKE '%Envio de correo%')
							OR source_meta IS NULL)
                            AND date_created >= CURRENT_DATE - INTERVAL '5 hour' 
                            ORDER BY date_created DESC";
            //string sqry = "SELECT * FROM internet_sale where email='luis117rojas@gmail.com' and source_meta is null and payed=false";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%mastercard%' AND date_created > '2022-12-07 23:37:00' ORDER BY date_created ASC";
            //string sqry = "SELECT payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider FROM internet_sale WHERE payment_provider LIKE '%aster%ard%' AND date_created >= now() - interval '70 minutes' AND source_meta IS NULL ORDER BY date_created ASC";
            //string sqry = $"select * from internet_sale where payment_provider like '%aster%ard%' AND NOT payment_provider like '%Gateway%' AND source_meta IS NULL AND date_created > '{formattedDateTime}' order by date_created desc";
            //string sqry = $"select * from internet_sale where payment_provider like '%aster%ard%' AND short_id='cbd626f2'";
            //string sqry = "select payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider from internet_sale is2 where \r\nextract (month from date_created)=extract(month from now()) and extract(year from date_created)=extract(year from now())\r\nand source_meta is null and upper(payment_provider) like upper('%aster%ard%')";
            //string sqry = "select payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider \r\nfrom internet_sale \r\nwhere date_created >='2023-04-01' \r\nand payment_provider like '%aster%ard%'\r\nand source_meta is null";
            try
            {
                tbl = mandaQry(sqry);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Logs.crealogs(DateTime.Now.ToString() + " Error ejecucion query: " + ex.Message);
            }

            if (tbl.Rows.Count == 0)
                Result = false;
            else
            {
                foreach (DataRow row in tbl.Rows)
                {
                    if (Convert.ToString(row["payed"]) == "False")
                    {
                        Logs.crealogs("\n" + DateTime.Now.ToString() + " Status internet_sale:" + row["payed"].ToString() + " short_id: " + row["short_id"].ToString());
                        DateTime creacion = Convert.ToDateTime(row["date_created"]);
                        verificacion = VerificarMasterCard(Convert.ToString(row["short_id"]));
                        if (verificacion == true)
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " Verificacion: " + verificacion.ToString());
                            try
                            {
                                string ticket = "";
                                while (ticket == "")
                                {
                                    ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                }
                                
                                Console.WriteLine("Se genero el ticket correctamente");
                                Logs.crealogs(DateTime.Now.ToString() + " Se genero el ticket correctamente: " + ticket);
                                origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                var temp_fechas = fechas.Split(',');
                                fecha_salida = temp_fechas[0];
                                fecha_llegada = temp_fechas[1];
                                Console.WriteLine("Exito se verifico el pago: " + short_id);
                                Logs.crealogs(DateTime.Now.ToString() + " Exito se verifico el pago: " + row["short_id"].ToString());
                                try
                                {
                                    EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                    concepto = "Validado y envio de correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + row["email"]);
                                }
                                catch
                                {
                                    concepto = "Error al enviar el correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al enviar el correo: " + row["email"]);
                                }
                            }
                            catch
                            {
                                if (op == "1")
                                {
                                    concepto = "Error al generar ticket para: " + short_id.ToString();
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Console.WriteLine("Error al generar ticket para: " + short_id);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al generar ticket para: " + short_id);
                                    EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    EnvioMail.EnviaCorreo("viridiana.fh@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    Logs.crealogs(DateTime.Now.ToString() + " Correo enviado a: luis.rojas@transportesmedrano.com viridiana.fh@transportesmedrano.com");
                                }

                            }
                        }
                        else
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " verificacion: " + verificacion.ToString());
                            DateTime fechaHora = DateTime.Parse(row["date_created"].ToString());
                            fechaHora = fechaHora.AddHours(+1);
                            if (op == "1" && DateTime.Now > fechaHora)
                            {
                                string sqryf = "UPDATE internet_sale SET source_meta='No se valido el boleto en 1 hr' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                mandaQry(sqryf);
                                try
                                {
                                    sqryf = $"SELECT * FROM trip_seat WHERE internet_sale_id='{Convert.ToString(row["id"])}'";
                                    DataTable tscr = mandaQry(sqryf);
                                    if(tscr.Rows.Count > 0)
                                    {
                                        Console.WriteLine();
                                        sqryf = $"insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status, F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id, f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name, 'Serv_Liberar_Asiento_245',f.trip_id,f.version,f.date_created, CURRENT_TIMESTAMP from trip_seat f where f.internet_sale_id = '{row["id"].ToString()}'";
                                        mandaQry(sqryf);
                                        Console.WriteLine();
                                        sqryf = $"DELETE FROM trip_seat WHERE internet_sale_id = '{row["id"].ToString()}'";
                                        mandaQry(sqryf);
                                        Console.WriteLine();
                                    }
                                    else
                                    {
                                        sqryf = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{Convert.ToString(row["id"])}' AND comments='Serv_Liberar_Asiento'";
                                        tscr = mandaQry(sqryf);
                                        if(tscr.Rows.Count > 0)
                                        {
                                            sqryf = "UPDATE cancel_reservation SET comments='Serv_Liberar_Asiento_245' WHERE internet_sale_id='" + Convert.ToString(row["id"]) + "'";
                                            mandaQry(sqryf);
                                            Console.WriteLine();
                                        }
                                    }
                                    
                                }
                                catch (Exception e)
                                {
                                    Logs.crealogs(DateTime.Now.ToString() + " " + e.Message);
                                }                                

                                Logs.crealogs(DateTime.Now.ToString() + " Tiempo agotado para: " + row["short_id"].ToString() + " No se valido el boleto en 1 hr.");
                            }
                        }
                    }
                    else if (Convert.ToString(row["payed"]) == "True")
                    {
                        Logs.crealogs("\n" + DateTime.Now.ToString() + " Status internet_sale: " + row["payed"]);
                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);

                        DataTable t = mandaQry($"SELECT t2.ticket_id FROM trip_seat t2 INNER JOIN internet_sale t1 ON t1.id=t2.internet_sale_id WHERE t1.id='{Convert.ToString(row["id"])}' LIMIT 1");
                        string ticket = "";
                        foreach (DataRow a in t.Rows)
                        {
                            ticket = a["ticket_id"].ToString();
                            if(ticket == "")
                            {
                                while (ticket == "")
                                {
                                    ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                }                                
                            }
                        }
                        bool v = false;
                        v = VerificarMasterCard(row["short_id"].ToString());
                        if (fechas != "0" && v == true)
                        {
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito solo se enviara el correo");
                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                            concepto = "Envio de correo";
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                            mandaQry(sqryf);
                            Logs.crealogs(DateTime.Now.ToString() + " Exito solo se enviara el correo: " + row["email"].ToString() + " Short_id: " + row["short_id"].ToString() + " Envio de correo.");
                        }
                    }
                }
            }
            Console.WriteLine("Eh terminado prrrrro");
            return true;
        }
        public static string ObtenerFecha(string short_id, string internet_sale_id, string tipo, string origen, string destino)
        {
            DateTime temp_departure, temp_ending;
            int aux_ruta = 0;
            int acum_minutes_starting = 0, acum_minutes_ending = 0;
            string fecha = "";
            string departure_date = "";
            string reverse = "";
            string descripcion = "";
            DataTable tbl = new DataTable();
            string sqry = "SELECT reverse,departure_date FROM trip t1 INNER JOIN trip_seat t2 ON t1.id=t2.trip_id WHERE t2.internet_sale_id='" + internet_sale_id + "' LIMIT 1";
            tbl = mandaQry(sqry);
            if (tbl.Rows.Count > 0)
            {
                foreach (DataRow row in tbl.Rows)
                {
                    departure_date = Convert.ToString(row["departure_date"]);
                    reverse = Convert.ToString(row["reverse"]);
                }
            }
            DataTable tbl2 = new DataTable();
            string sqry2 = "select * from stop_off where route_id like (select t2.route_id from trip_seat t1 inner join stop_off t2 on t1.starting_stop_id=t2.id inner join stop_off t3 on t1.ending_stop_id=t3.id where  t1.internet_sale_id like '" + internet_sale_id + "' LIMIT 1) order by order_index asc";
            tbl2 = mandaQry(sqry2);
            if (tbl2.Rows.Count > 0)
            {
                foreach (DataRow row in tbl2.Rows)
                {
                    if (reverse == "False")
                    {
                        if (Convert.ToString(row["name"]) == origen)
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                            aux_ruta = 1;
                        }
                        else if (aux_ruta == 1)
                        {
                            if (Convert.ToString(row["name"]) == destino)
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                aux_ruta = 0;
                                break;
                            }
                            else
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                acum_minutes_ending += Convert.ToInt32(row["waiting_minutes"]);
                            }
                        }
                        else
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                        }
                    }
                    else if (reverse == "True")
                    {
                        if (Convert.ToString(row["name"]) == destino)
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                            aux_ruta = 1;
                        }
                        else if (aux_ruta == 1)
                        {
                            if (Convert.ToString(row["name"]) == origen)
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                aux_ruta = 0;
                                break;
                            }
                            else
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                acum_minutes_ending += Convert.ToInt32(row["waiting_minutes"]);
                            }
                        }
                        else
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                        }
                    }
                }
                temp_departure = Convert.ToDateTime(departure_date).AddMinutes(acum_minutes_starting);
                temp_ending = Convert.ToDateTime(temp_departure).AddMinutes(acum_minutes_ending);
                temp_departure = temp_departure.AddHours(-6);
                temp_ending = temp_ending.AddHours(-6);
                fecha = temp_departure.ToString("dd-MM-yyyy HH:mm:ss") + "," + temp_ending.ToString("dd-MM-yyyy HH:mm:ss");
            }
            else
            {
                fecha = "0";
            }
            return fecha;
        }
        public static string ObtenerLugar(string shorId, string internetSaleId, string tipo)
        {
            string descripcion = "";
            if (tipo == "destino")
            {
                DataTable tbl = new DataTable();
                string sqry = "select so.name from trip_seat ts inner join stop_off so on ts.ending_stop_id = so.id where ts.internet_sale_id like '" + internetSaleId + "'";
                tbl = mandaQry(sqry);
                string destino = "";
                if (tbl.Rows.Count > 0)
                {
                    foreach (DataRow row in tbl.Rows)
                    {
                        descripcion = Convert.ToString(row["name"]);
                    }
                }
            }
            else if (tipo == "origen")
            {
                string origen = "";
                DataTable tbl = new DataTable();
                string sqry = "SELECT name FROM stop_off t1 INNER JOIN trip_seat t2 ON t1.id=t2.starting_stop_id WHERE t2.internet_sale_id='" + internetSaleId + "'";
                tbl = mandaQry(sqry);
                foreach (DataRow row in tbl.Rows)
                {
                    descripcion = Convert.ToString(row["name"]);
                }
            }
            return descripcion;
        }

        public static string GenerarBoleto(string internet_sale_id)
        {
            string res = "";
            DateTime fecha = DateTime.Now;
            //fecha.AddHours(-6);
            /*
            DataTable tbl1 = new DataTable();
            string sqry2 = "UPDATE detail_sale SET status_payment=true,date_updated='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' WHERE id_detail=" + id_detail;
            tbl1 = mandaQry(sqry2);*/
            int tick = 0, cont = 0;
            string GetBol = "SELECT * FROM trip_seat WHERE internet_sale_id='" + internet_sale_id + "'";
            DataTable tbl2 = new DataTable();
            tbl2 = mandaQry(GetBol);
            string ticketid = GenShortId();
            if (tbl2.Rows.Count == 0)
            {
                string query = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{internet_sale_id}'";
                DataTable tbl3 = new DataTable();
                tbl3 = mandaQry(query);
                if(tbl3.Rows.Count > 0)
                {
                     query = $@"insert into trip_seat(id,version,date_created,last_updated,seat_id,status,starting_stop_id,ending_stop_id,
					  internet_sale_id,trip_id,seat_name,passenger_name,ticket_id,comments,passenger_type,user_id,sold_price,
					  payed_price,original_price)
                      select id,version,date_created,last_updated,seat_id,'OCCUPIED',starting_stop_id,ending_stop_id,
					  internet_sale_id,trip_id,seat_name,passenger_name,null,'Regreso validacion 2',passenger_type,user_id,sold_price,
					  payed_price,sold_price  from cancel_reservation where internet_sale_id='{internet_sale_id}' LIMIT 1";

                    query = $"UPDATE internet_sale SET source_meta='Validado y envio de correo', payed=true WHERE id='{internet_sale_id}'";
                    mandaQry(query);

                    tbl2 = new DataTable();
                    mandaQry(query);
                    tbl2 = mandaQry(GetBol);
                }                
            }

            foreach (DataRow row3 in tbl2.Rows)
            {                
                res = ticketid;
                while (tick == 0)
                {
                    string qry21 = "SELECT * FROM trip_seat WHERE ticket_id='" + ticketid + "'";
                    DataTable tbl3 = new DataTable();
                    tbl3 = mandaQry(qry21);
                    foreach (DataRow row21 in tbl3.Rows)
                    {
                        ticketid = GenShortId();
                        cont++;
                    }
                    if (cont == 0)
                        tick = 1;
                }
                try
                {
                    string qry2 = "UPDATE trip_seat set status='OCCUPIED',ticket_id='" + ticketid + "',version=1 WHERE id='" + row3["id"] + "'";
                    mandaQry(qry2);
                }
                catch { }
                ticketid = GenShortId();
            }
            try
            {
                string qry3 = "UPDATE internet_sale set version=2,last_updated='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "',payed='true' WHERE id='" + internet_sale_id + "';";
                mandaQry(qry3);
            }
            catch { }
            return res;
        }
        public static string GenShortId()
        {
            var sh = "";
            var characters = "ab0cde1fghi2jkl3mno4pqrs5tuvw6xyz789";
            var Charsarr = new char[8];
            var random = new Random();
            for (int i = 0; i < Charsarr.Length; i++)
            {
                sh = sh + characters[random.Next(characters.Length)];
            }
            return sh;
        }
        public static bool VerificarMasterCard(string short_id)
        {
            string result = "";
            bool res = false;
            var url = "https://banamex.dialectpayments.com/api/rest/version/62/merchant/1079224/order/" + short_id;

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.Headers["Authorization"] = "Basic bWVyY2hhbnQuMTA3OTIyNDpjNGUyYzAwY2JjYjc5NDNmOTEyZGZmZTI1ZDFiNTc1ZQ==";
            try
            {
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    result = (result.Replace("\n", "")).ToString();
                    result = "[" + result + "]";

                }
                var rs = JsonConvert.DeserializeObject<List<ResMasterCard>>(result);
                foreach (var item in rs)
                {
                    if ((Convert.ToString(rs[0].result) == "SUCCESS") && (Convert.ToString(rs[0].status) == "CAPTURED"))
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Verificacion correcta: " + rs[0].result.ToString() + " " + rs[0].status.ToString() + " para: " + "short_id: " + short_id);
                        res = true;
                    }
                    else
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Verificacion incorrecta: " + rs[0].result.ToString() + " " + rs[0].status.ToString() + " para: " + "short_id: " + short_id);
                        res = false;
                    }
                }
            }
            catch
            {
                res = false;
            }
            return res;
        }
        #endregion MasterCard
        #region Paypal
        public static bool BuscarPagosPayPal(string starting_day, string ending_day, string op)
        {
            Console.WriteLine("################----PayPal----#######################");
            Logs.crealogs("\n" + DateTime.Now.ToString() + " ################----PayPal----#######################");
            //2022-12-06 23:24:15.2540000
            string correo_envio = "", short_id = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime hora_actual = DateTime.Now;
            string concepto = "";
            hora_actual = hora_actual.AddMinutes(-30);
            Console.WriteLine("Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            Logs.crealogs(DateTime.Now.ToString() + " Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            //Console.WriteLine(hora_actual);
            string dia_actual = Convert.ToInt32(hora_actual.Day) < 10 ? "0" + (hora_actual.Day).ToString() : (hora_actual.Day).ToString();
            DateTime fechai = DateTime.Parse(starting_day);
            string fechaFormateadai = fechai.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
            DateTime fechaf = DateTime.Parse(ending_day);
            string fechaFormateadaf = fechaf.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
            string hora_inicio = hora_actual.Year + "-" + hora_actual.Month + "-" + dia_actual + " " + hora_actual.TimeOfDay.Hours + ":" + hora_actual.TimeOfDay.Minutes + ":00";
            bool verificacion = false;
            DataTable tbl = new DataTable();
            bool Result = true;
            Console.WriteLine("Inicio: " + hora_inicio);
            DateTime dateTime = DateTime.Now; // Obtén la hora actual

            string formattedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffzzz");
            string formattedDateTimem = dateTime.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss.fffzzz");
            //string sqry = "SELECT * FROM internet_sale WHERE id='024da958-d486-4d02-b09c-5cd379eaa8fb'";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%paypal%' AND date_created > '2022-12-07 10:36:00' ORDER BY date_created ASC";
            //string sqry = "select * from internet_sale where email like '%linkrodriguez897@gmail.com%' order by date_created desc";
            string sqry = $"SELECT * FROM internet_sale WHERE (payment_provider LIKE '%paypal%' OR payment_provider LIKE '%ay%al%' OR payment_provider like '%mastercardGateway%') AND date_created > '{formattedDateTimem}' AND  date_created < '{formattedDateTime}' AND ((source_meta NOT LIKE 'Validado y envio de correo' AND source_meta NOT LIKE '%Envio de correo%') OR source_meta IS NULL) ORDER BY date_created ASC ";
            //string sqry = "SELECT * FROM internet_sale WHERE short_id='98f8e170'";
            try
            {
                tbl = mandaQry(sqry);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Logs.crealogs(DateTime.Now.ToString() + " Error al ejecutar query: " + ex.Message);
            }

            if (tbl.Rows.Count == 0)
                Result = false;
            else
            {
                foreach (DataRow row in tbl.Rows)
                {
                    if (Convert.ToString(row["payed"]) == "False")
                    {
                        Logs.crealogs("\n" + DateTime.Now.ToString() + " status internet_sale: " + row["payed"].ToString() + " id: " + row["id"].ToString());
                        DateTime creacion = Convert.ToDateTime(row["date_created"]);
                        verificacion = VerificarPayPal(Convert.ToString(row["full_response"]), Convert.ToString(row["total_amount"]), Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                        if (verificacion == true)
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " verificacion correcta: " + verificacion.ToString());
                            try
                            {
                                string ticket = "";
                                while (ticket == "")
                                {
                                    ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                }
                                    
                                Console.WriteLine("Se genero el ticket correctamente");
                                Logs.crealogs(DateTime.Now.ToString() + " Se genero el ticket correctamente: " + ticket);
                                origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                var temp_fechas = fechas.Split(',');
                                fecha_salida = temp_fechas[0];
                                fecha_llegada = temp_fechas[1];
                                Console.WriteLine("Exito se verifico el pago: " + short_id);
                                Logs.crealogs(DateTime.Now.ToString() + " Exito se verifico el pago: " + row["short_id"].ToString());
                                try
                                {
                                    EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                    concepto = "Validado y envio de correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + row["email"].ToString());
                                }
                                catch
                                {
                                    concepto = "Error al enviar el correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al enviar el correo: " + row["email"].ToString());
                                }
                            }
                            catch
                            {
                                if (op == "1")
                                {
                                    concepto = "Error al generar ticket para: " + short_id.ToString();
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Console.WriteLine("Error al generar ticket para: " + short_id);
                                    EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    EnvioMail.EnviaCorreo("viridiana.fh@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al generar ticket para: " + row["short_id"].ToString() + "Envio correo a luis.rojas@transportesmedrano.com viridiana.fh@transportesmedrano.com");
                                }
                            }

                        }
                        else
                        {
                            DateTime fechaHora = DateTime.Parse(row["date_created"].ToString());
                            fechaHora = fechaHora.AddHours(+1);
                            if (op == "1" && DateTime.Now > fechaHora)
                            {
                                concepto = "No se valido el boleto en 1 hr";

                                string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                mandaQry(sqryf);
                                
                                try
                                {
                                    sqryf = $"SELECT * FROM trip_seat WHERE internet_sale_id='{Convert.ToString(row["id"])}'";
                                    DataTable tscr = mandaQry(sqryf);
                                    if (tscr.Rows.Count > 0)
                                    {
                                        Console.WriteLine();
                                        sqryf = $"insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status, F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id, f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name, 'Serv_Liberar_Asiento_245',f.trip_id,f.version,f.date_created, CURRENT_TIMESTAMP from trip_seat f where f.internet_sale_id = '{row["id"].ToString()}'";
                                        mandaQry(sqryf);
                                        Console.WriteLine();
                                        sqryf = $"DELETE FROM trip_seat WHERE internet_sale_id = '{row["id"].ToString()}'";
                                        mandaQry(sqryf);
                                        Console.WriteLine();
                                    }
                                    else
                                    {
                                        sqryf = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{Convert.ToString(row["id"])}' AND comments='Serv_Liberar_Asiento'";
                                        tscr = mandaQry(sqryf);
                                        if (tscr.Rows.Count > 0)
                                        {
                                            sqryf = "UPDATE cancel_reservation SET comments='Serv_Liberar_Asiento_245' WHERE internet_sale_id='" + Convert.ToString(row["id"]) + "'";
                                            mandaQry(sqryf);
                                            Console.WriteLine();
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logs.crealogs(DateTime.Now.ToString() + " " + e.Message);
                                }

                                Console.WriteLine("Error al generar ticket para: " + short_id);
                                Logs.crealogs(DateTime.Now.ToString() + " Se agoto el tiempo para: " + row["short_id"].ToString() + " No se valido el boleto en 1 hr");
                            }
                        }
                    }
                    else if (Convert.ToString(row["payed"]) == "True")
                    {
                        Logs.crealogs("\n" + DateTime.Now.ToString() + " status internet_sale: " + row["payed"].ToString());
                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);

                        DataTable t = mandaQry($"SELECT t2.ticket_id FROM trip_seat t2 INNER JOIN internet_sale t1 ON t1.id=t2.internet_sale_id WHERE t1.id='{Convert.ToString(row["id"])}' LIMIT 1");
                        string ticket = "";
                        foreach (DataRow a in t.Rows)
                        {
                            ticket = a["ticket_id"].ToString();
                            if (ticket == "")
                            {
                                while (ticket == "")
                                {
                                    ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                }
                            }
                        }
                        bool v = VerificarPayPal(Convert.ToString(row["full_response"]), Convert.ToString(row["total_amount"]), Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                        if (fechas != "0" && v == true)
                        {
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito solo se enviara el correo");
                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                            concepto = "Envio de correo";
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                            mandaQry(sqryf);
                            Logs.crealogs(DateTime.Now.ToString() + " Exito solo se enviara el correo: " + row["email"].ToString() + " para: " + row["short_id"].ToString());
                        }
                    }
                }
            }
            Console.WriteLine("Eh terminado prrrrro");
            return true;
        }
        public static bool VerificarPayPal(string full_response, string total, string shortId, string internet_sale_id)
        {
            bool res = false;
            string responseBody = "";
            JObject response = JObject.Parse(full_response);
            if ((response != null) && (full_response.Contains("paypal")))
            {//token=9C06111807057224B
             //Access_token=A21AAOJO48ClEjqvzhmz351mNQ5C-Yg-XQrYWtyWncoPUxkKBhmdzQ1gGREfgNAmZAx5IjBHZZiYoZDFOUwmP-6sTA1rbXJZg
                string acces_token = Convert.ToString(response.Root);
                string token = "";
                var temp_url = acces_token.Split('?');
                temp_url = Convert.ToString(temp_url[1]).Split('&');
                var temp_acces_token = Convert.ToString(temp_url[0]).Split('=');
                acces_token = Convert.ToString(temp_acces_token[1]);
                var tem_token = Convert.ToString(temp_url[1]).Split('=');
                var tem_token2 = Convert.ToString(temp_url[1]).Split('\"');
                var tem_token3 = Convert.ToString(tem_token2[0]).Split('=');
                token = Convert.ToString(tem_token3[1]);
                var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/CapturaPaypal?id=" + acces_token + "&access_token=" + token + "&boletos=1&monto=" + total);
                requestToken.ContentType = "application/json";
                requestToken.Method = "POST";
                requestToken.ContentLength = 0;
                try
                {
                    using (WebResponse responseToken = requestToken.GetResponse())
                    {
                        using (Stream strReader = responseToken.GetResponseStream())
                        {
                            if (strReader == null) return res;
                            using (StreamReader objReader = new StreamReader(strReader))
                            {
                                responseBody = objReader.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error al capturar pago");
                    Logs.crealogs(DateTime.Now.ToString() + " Error al capturar pago");
                    res = false;
                }
                responseBody = ConfirmarPago(token, acces_token, total, shortId, internet_sale_id);
                Console.WriteLine(responseBody);
                if (responseBody != "")
                    res = true;
                else
                    res = false;
            }
            else
            {
                res = false;
            }
            return res;
        }
        public static string ConfirmarPago(string access_token, string token, string amount, string shortId, string internet_sale_id)
        {
            string responseBody = null;
            var requestToken = (HttpWebRequest)WebRequest.Create("http://transmed.dyndns-web.com:400/Home/RevisarPaypal?id=" + token + "&access_token=" + access_token);
            requestToken.ContentType = "application/json";
            requestToken.Method = "GET";
            requestToken.ContentLength = 0;
            try
            {
                using (WebResponse responseToken = requestToken.GetResponse())
                {
                    using (Stream strReader = responseToken.GetResponseStream())
                    {
                        if (strReader == null) return responseBody;
                        using (StreamReader objReader = new StreamReader(strReader))
                        {
                            responseBody = objReader.ReadToEnd();
                            responseBody = "[" + responseBody + "]";
                            var rs = JsonConvert.DeserializeObject<List<RevPayPal>>(responseBody);
                            if (Convert.ToString(rs[0].status) == "COMPLETED")
                            {
                                responseBody = CapturarPayPal(shortId, internet_sale_id, amount);
                                Logs.crealogs(DateTime.Now.ToString() + " status pago: COMPLETED");
                            }
                            else
                            {
                                responseBody = "";
                                Logs.crealogs(DateTime.Now.ToString() + " status pago: " + rs[0].status.ToString());
                            }
                        }
                    }
                }
            }
            catch
            {
                responseBody = "";
            }
            return responseBody;
        }
        public static string CapturarPayPal(string short_id, string internet_sale_id, string amount)
        {
            string responseBody = null;
            var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/UpdateBoletoMC?shortId=" + short_id + "&sale_id=" + internet_sale_id + "&amount=" + amount);
            requestToken.ContentType = "application/json";
            requestToken.Method = "POST";
            requestToken.ContentLength = 0;
            using (WebResponse responseToken = requestToken.GetResponse())
            {
                using (Stream strReader = responseToken.GetResponseStream())
                {
                    if (strReader == null) return responseBody;
                    using (StreamReader objReader = new StreamReader(strReader))
                    {
                        responseBody = objReader.ReadToEnd();
                    }
                }
                return responseBody;
            }
        }
        #region clases
        public class Amount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
            public Breakdown breakdown { get; set; }
        }

        public class Breakdown
        {
            public ItemTotal item_total { get; set; }
        }

        public class Item
        {
            public string name { get; set; }
            public UnitAmount unit_amount { get; set; }
            public string quantity { get; set; }
            public string description { get; set; }
        }

        public class ItemTotal
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }

        public class Link
        {
            public string href { get; set; }
            public string rel { get; set; }
            public string method { get; set; }
        }

        public class Payee
        {
            public string email_address { get; set; }
            public string merchant_id { get; set; }
        }

        public class PurchaseUnit
        {
            public string reference_id { get; set; }
            public Amount amount { get; set; }
            public Payee payee { get; set; }
            public List<Item> items { get; set; }
        }

        public class RevPayPal
        {
            public string id { get; set; }
            public string intent { get; set; }
            public string status { get; set; }
            public List<PurchaseUnit> purchase_units { get; set; }
            public DateTime create_time { get; set; }
            public List<Link> links { get; set; }
        }

        public class UnitAmount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }
        #endregion clases
        #region clasesPaypal
        public class Token
        {
            public string token { get; set; }
            public string access { get; set; }
        }
        #endregion clasesPaypal

        #endregion Paypal
        #region paynet
        public static bool BuscarPagosPaynet()
        {
            try
            {
                Console.WriteLine("################----PayNet----#######################");
                Logs.crealogs("\n" + DateTime.Now.ToString() + " ################----PayNet----#######################");
                bool verificacion = false;
                DataTable tbl = new DataTable();
                bool Result = true;
                string sqry = "select t1.id_pago,t1.internet_sale_id,t2.short_id,t2.email,t2.total_amount from pagos_procesados t1 \r\ninner join internet_sale t2 on t1.internet_sale_id=t2.id \r\nwhere t1.id_estado = 1";
                tbl = mandaQry(sqry);
                if (tbl.Rows.Count == 0) return false;
                foreach (DataRow row in tbl.Rows)
                {
                    var id_pago = row["id_pago"].ToString();
                    var internet_sale_id = row["internet_sale_id"].ToString();
                    var short_id = row["short_id"].ToString();
                    var email = row["email"].ToString();
                    var total_amount = Convert.ToDecimal(row["total_amount"]);
                    verificacion = VerificarPaynet(id_pago, internet_sale_id, short_id, email, total_amount);
                }
                Console.WriteLine("Eh terminado perrrrrro");
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool VerificarPaynet(string idPago, string internet_sale_id, string short_id, string email, decimal total_amount)
        {
            var url = "https://api.openpay.mx/v1/maktyd38uvrqdhlfi4qz/charges/" + idPago;
            var result = "";
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            string concepto = "";
            string correo_envio = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime fecha = DateTime.Now;

            httpRequest.Headers["Authorization"] = "Basic c2tfYjllY2RhNGZiZjhmNGVhZTlmMjIyNmQ5N2NmNjIyNzQ6UzFzdDNtQDU=";
            try
            {
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    result = (result.Replace("\n", "").ToString());
                    result = "[" + result + "]";
                }
                var rs = JsonConvert.DeserializeObject<List<openpay>>(result);
                foreach (var item in rs)
                {
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "COMPLETED")
                    {
                        try
                        {
                            Logs.crealogs("\n" + DateTime.Now.ToString() + " Se verifico el pago: " + rs[0].status.ToString() + " para: " + short_id);
                            string sqry = "UPDATE pagos_procesados set id_estado=2,enviado_correo=1,estatus='PAGADO',fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                            mandaQry(sqry);
                            string ticket = GenerarBoleto(internet_sale_id);
                            Console.WriteLine("Se genero el ticket correctamente");
                            origen = ObtenerLugar(short_id, internet_sale_id, "origen");
                            destino = ObtenerLugar(short_id, internet_sale_id, "destino");
                            fechas = ObtenerFecha(short_id, internet_sale_id, "llegada", origen, destino);
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito se verifico el pago: " + short_id);
                            Logs.crealogs(DateTime.Now.ToString() + " Se genero el ticket correctamente: " + ticket);
                            try
                            {
                                EnvioMail.EnviaCorreo(email, ticket, short_id, origen, destino, fecha_salida, fecha_llegada, decimal.Round(total_amount));
                                concepto = "Validado y envio de correo";
                                string sqryf = "UPDATE internet_sale SET payment_provider ='paynet', source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                                mandaQry(sqryf);
                                Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + email);
                            }
                            catch
                            {
                                concepto = "Error al enviar el correo";
                                string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                                mandaQry(sqryf);
                                Logs.crealogs(DateTime.Now.ToString() + " Errror al enviar correo a : " + email);
                                //string sqry = "UPDATE pagos_procesados set id_estado=2,enviado_correo=1,estatus='PAGADO',fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                                //mandaQry(sqry);
                            }
                        }
                        catch
                        {
                            concepto = "Error al generar ticket para: " + short_id.ToString();
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                            mandaQry(sqryf);
                            Console.WriteLine("Error al generar ticket para: " + short_id);
                            EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", "", short_id, "", "", "", "", 0);
                            EnvioMail.EnviaCorreo("viridiana.fh@transportesmedrano.com", "", short_id, "", "", "", "", 0);
                            Logs.crealogs(DateTime.Now.ToString() + " Error al generar ticket para: " + short_id.ToString() + "Correo enviado a luis.rojas@transportesmedrano.com viridiana.fh@transportesmedrano.com");
                        }
                    }
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "CANCELLED")
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Se cancelo el pago para: " + short_id + " status: " + rs[0].status.ToString());
                        string sqry = "UPDATE pagos_procesados set id_estado=3,estatus='CANCELADO' ,fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                        mandaQry(sqry);
                        try
                        {
                            sqry = "insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status,F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id,f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name,'Serv_Liberar_Asiento_245',f.trip_id,f.version,f.date_created,CURRENT_TIMESTAMP from trip_seat f where internet_sale_id = '" + internet_sale_id + "'";
                            mandaQry(sqry);
                            Logs.crealogs(DateTime.Now.ToString() + " Boleto movido a cancel reservation: " + internet_sale_id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Mover a Cancel_reservation: " + ex.Message);
                            Logs.crealogs(DateTime.Now.ToString() + " Ocurrio un error al mover a cancel reservation: ");
                        }
                        try
                        {
                            sqry = "DELETE FROM trip_seat WHERE internet_sale_id = '" + internet_sale_id + "';";
                            mandaQry(sqry);
                            Logs.crealogs(DateTime.Now.ToString() + " Bolet eliminado de trip seat: " + internet_sale_id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Eliminar");
                            Logs.crealogs(DateTime.Now.ToString() + " Error al eliminar de trip seat: " + ex.Message);
                        }
                        Console.WriteLine("Pago Cancelado Para" + short_id);
                        Logs.crealogs(DateTime.Now.ToString() + " Pago Cancelado Para" + short_id);
                    }
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "IN_PROGRESS")
                    {
                        Logs.crealogs("\n" + DateTime.Now.ToString() + " En espera de pago para: " + short_id + " status: " + rs[0].status.ToString());
                        DataTable tbl = new DataTable();
                        DataTable tbl2 = new DataTable();
                        DateTime dateTime = DateTime.Now;
                        dateTime = dateTime.AddHours(2);
                        DateTime departure_date_corrida = DateTime.Now;
                        string sqry = $"SELECT * FROM trip_seat WHERE internet_sale_id='{internet_sale_id}'";
                        tbl = mandaQry(sqry);
                        foreach (DataRow fila in tbl.Rows)
                        {
                            string siu = fila["trip_id"].ToString();
                            sqry = $"SELECT * FROM trip WHERE id='{siu}'";
                            tbl2 = mandaQry(sqry);
                            foreach (DataRow fila2 in tbl2.Rows)
                            {
                                departure_date_corrida = DateTime.Parse(fila2["departure_date"].ToString());
                                //departure_date_corrida = departure_date_corrida.AddHours(-6);


                                if (dateTime >= departure_date_corrida)
                                {
                                    try
                                    {
                                        sqry = "update pagos_procesados set id_estado=3,estatus='CANCELADO',fecha_actualizacion='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where internet_sale_id like '" + internet_sale_id + "'";
                                        mandaQry(sqry);
                                        Logs.crealogs(DateTime.Now.ToString() + " Limite de tiempo exedido se liberara asiento: " + internet_sale_id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Error actualizar status pagos procesado: " + ex.Message);
                                    }
                                    try
                                    {
                                        sqry = "insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status,F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id,f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name,'Serv_Liberar_Asiento_245',f.trip_id,f.version,f.date_created,CURRENT_TIMESTAMP from trip_seat f where internet_sale_id = '" + internet_sale_id + "'";
                                        mandaQry(sqry);
                                        Logs.crealogs(DateTime.Now.ToString() + " Asiento liberado correctamente: " + internet_sale_id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Ocurrio un error al liberar el asiento: " + internet_sale_id);
                                    }
                                    try
                                    {
                                        sqry = "DELETE FROM trip_seat WHERE internet_sale_id = '" + internet_sale_id + "';";
                                        mandaQry(sqry);
                                        Logs.crealogs(DateTime.Now.ToString() + " Boleto eliminado de trip_seat: " + internet_sale_id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Ocurrio un error al eliminar de trip_seat: " + internet_sale_id);
                                    }
                                }
                                else
                                {
                                    sqry = "update pagos_procesados set id_estado=1,estatus='PENDIENTE',fecha_actualizacion='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where internet_sale_id like '" + internet_sale_id + "'";
                                    mandaQry(sqry);
                                    Console.WriteLine("Pago Pendiente Para" + short_id);
                                    Logs.crealogs(DateTime.Now.ToString() + " Pago Pendiente Para" + short_id);
                                }
                            }
                        }

                    }
                }
                Console.WriteLine(httpResponse.StatusCode);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static DataTable mandaQry(string sqry)
        {
            var cadenaConexion = "server = medrano-prod.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=sBMH8fvnPM;";
            NpgsqlConnection conexion = new NpgsqlConnection(cadenaConexion);
            NpgsqlCommand comando;
            NpgsqlDataAdapter adaptador;
            DataTable tbl = new DataTable();

            string Result = string.Empty;

            if (!string.IsNullOrWhiteSpace(cadenaConexion))
            {

                comando = new NpgsqlCommand(sqry, conexion);
                comando.CommandType = CommandType.Text;
                conexion.Open();
                comando.ExecuteNonQuery();//Revisar, se ejecuta de nuevo el query
                conexion.Close();
                tbl = new DataTable();
                if (sqry.StartsWith("SELECT") || sqry.StartsWith("select"))
                {
                    adaptador = new NpgsqlDataAdapter(comando);
                    adaptador.Fill(tbl);
                }
                Result = "Conectado Exitoso";
            }
            return tbl;
        }
        #endregion paynet
        #region ClassMasterCard
        #region ErrorPago
        public class Error
        {
            public string cause { get; set; }
            public string explanation { get; set; }
        }

        public class ErrorPago
        {
            public Error error { get; set; }
            public string result { get; set; }
        }
        #endregion ErrorPago
        public class _3ds
        {
            public string acsEci { get; set; }
            public string authenticationToken { get; set; }
            public string transactionId { get; set; }
        }

        public class _3ds22
        {
            public string acsTransactionId { get; set; }
            public string directoryServerId { get; set; }
            public string dsTransactionId { get; set; }
            public bool methodCompleted { get; set; }
            public string methodSupported { get; set; }
            public string protocolVersion { get; set; }
            public string requestorId { get; set; }
            public string requestorName { get; set; }
            public string transactionStatus { get; set; }
        }

        public class Acquirer
        {
            public string merchantId { get; set; }
            public int? batch { get; set; }
            public string id { get; set; }
        }

        public class Authentication
        {
            public string acceptVersions { get; set; }
            public string channel { get; set; }
            public string method { get; set; }
            public string payerInteraction { get; set; }
            public string purpose { get; set; }
            public Redirect redirect { get; set; }
            public string version { get; set; }
            public string transactionId { get; set; }
        }

        public class Card
        {
            public string brand { get; set; }
            public Expiry expiry { get; set; }
            public string fundingMethod { get; set; }
            public string issuer { get; set; }
            public string nameOnCard { get; set; }
            public string number { get; set; }
            public string scheme { get; set; }
            public string storedOnFile { get; set; }
        }

        public class CardSecurityCode
        {
            public string gatewayCode { get; set; }
        }

        public class Chargeback
        {
            public int amount { get; set; }
            public string currency { get; set; }
        }

        public class Customer
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class Device
        {
            public string browser { get; set; }
            public string ipAddress { get; set; }
        }

        public class Expiry
        {
            public string month { get; set; }
            public string year { get; set; }
        }

        public class Order
        {
            public double amount { get; set; }
            public string authenticationStatus { get; set; }
            public Chargeback chargeback { get; set; }
            public DateTime creationTime { get; set; }
            public string currency { get; set; }
            public string id { get; set; }
            public DateTime lastUpdatedTime { get; set; }
            public double merchantAmount { get; set; }
            public string merchantCategoryCode { get; set; }
            public string merchantCurrency { get; set; }
            public string status { get; set; }
            public double totalAuthorizedAmount { get; set; }
            public double totalCapturedAmount { get; set; }
            public double totalDisbursedAmount { get; set; }
            public double totalRefundedAmount { get; set; }
            public ValueTransfer valueTransfer { get; set; }
            public string customerReference { get; set; }
            public string description { get; set; }
        }

        public class Provided
        {
            public Card card { get; set; }
        }

        public class Redirect
        {
            public string domainName { get; set; }
        }

        public class Response
        {
            public string gatewayCode { get; set; }
            public string gatewayRecommendation { get; set; }
            public string acquirerCode { get; set; }
            public CardSecurityCode cardSecurityCode { get; set; }
        }

        public class ResMasterCard
        {
            public string _3dsAcsEci { get; set; }
            public double amount { get; set; }
            public Authentication authentication { get; set; }
            public string authenticationStatus { get; set; }
            public string authenticationVersion { get; set; }
            public Chargeback chargeback { get; set; }
            public DateTime creationTime { get; set; }
            public string currency { get; set; }
            public Customer customer { get; set; }
            public string customerReference { get; set; }
            public string description { get; set; }
            public Device device { get; set; }
            public string id { get; set; }
            public DateTime lastUpdatedTime { get; set; }
            public string merchant { get; set; }
            public double merchantAmount { get; set; }
            public string merchantCategoryCode { get; set; }
            public string merchantCurrency { get; set; }
            public string result { get; set; }
            public SourceOfFunds sourceOfFunds { get; set; }
            public string status { get; set; }
            public double totalAuthorizedAmount { get; set; }
            public double totalCapturedAmount { get; set; }
            public double totalDisbursedAmount { get; set; }
            public double totalRefundedAmount { get; set; }
            public List<Transaction> transaction { get; set; }
        }

        public class SourceOfFunds
        {
            public Provided provided { get; set; }
            public string type { get; set; }
        }

        public class Transaction
        {
            public Authentication authentication { get; set; }
            public Customer customer { get; set; }
            public Device device { get; set; }
            public string merchant { get; set; }
            public Order order { get; set; }
            public Response response { get; set; }
            public string result { get; set; }
            public SourceOfFunds sourceOfFunds { get; set; }
            public DateTime timeOfLastUpdate { get; set; }
            public DateTime timeOfRecord { get; set; }
            public Transaction transaction { get; set; }
            public string version { get; set; }
            public string gatewayEntryPoint { get; set; }
            public Acquirer acquirer { get; set; }
            public double amount { get; set; }
            public string authenticationStatus { get; set; }
            public string currency { get; set; }
            public string id { get; set; }
            public string stan { get; set; }
            public string type { get; set; }
            public string authorizationCode { get; set; }
            public string receipt { get; set; }
            public string source { get; set; }
            public double? taxAmount { get; set; }
            public string terminal { get; set; }
        }

        public class ValueTransfer
        {
            public string accountType { get; set; }
        }
        #endregion ClassMasterCard
        public class Ordenes
        {
            public string id { get; set; }
            public string intent { get; set; }
            public string status { get; set; }
            public string processing_instruction { get; set; }
            public purchase_units purchase_units { get; set; }
            public payer payer { get; set; }
            public string update_time { get; set; }

            public List<links> links { get; set; }
        }
        public class purchase_units
        {
            public string reference_id { get; set; }
            public amount amount { get; set; }
            public payee payee { get; set; }
            public string description { get; set; }
            public List<items> items { get; set; }
            public shipping shipping { get; set; }
            public payments payments { get; set; }
        }
        public class amount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
            public breakdown breakdown { get; set; }

        }
        public class breakdown
        {
            public item_total item_total { get; set; }
            public item_total shipping { get; set; }
            public item_total handling { get; set; }
            public item_total insurance { get; set; }
            public item_total shipping_discount { get; set; }
            public item_total discount { get; set; }
        }
    }
    public class item_total
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }
    public class payee
    {
        public string email_address { get; set; }
        public string merchant_id { get; set; }

    }
    public class items
    {
        public string name { get; set; }
        public item_total unit_amount { get; set; }
        public item_total tax { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
    }
    public class shipping
    {
        public address address { get; set; }
    }
    public class address
    {
        public string name { get; set; }

    }
    public class payments
    {
        public List<captures> captures { get; set; }
    }
    public class captures
    {
        public string id { get; set; }
        public string status { get; set; }
        public item_total amount { get; set; }
        public string final_capture { get; set; }
        public seller_protection seller_protection { get; set; }
        public seller_receivable_breakdown seller_receivable_breakdown { get; set; }
        public List<links> links { get; set; }
        public string create_time { get; set; }
        public string update_time { get; set; }
    }
    public class seller_protection
    {
        public string status { get; set; }
        public List<string> dispute_categories { get; set; }
    }
    public class seller_receivable_breakdown
    {
        public item_total gross_amount { get; set; }
        public item_total paypal_fee { get; set; }
        public item_total net_amount { get; set; }
    }
    public class links
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }

    }
    public class payer
    {

        public string email_address { get; set; }
        public string payer_id { get; set; }
        public address1 address { get; set; }
    }
    public class address1
    {
        public string country_code { get; set; }
    }
    public class openpay
    {
        public string id { get; set; }
        public string authorization { get; set; }
        public string operation_type { get; set; }
        public string transaction_type { get; set; }
        public string status { get; set; }
        public bool conciliated { get; set; }
        public DateTime creation_date { get; set; }
        public DateTime operation_date { get; set; }
        public string description { get; set; }
        public string error_message { get; set; }
        public string order_id { get; set; }
        public string customer_id { get; set; }
        public DateTime due_date { get; set; }
        public double amount { get; set; }
        public fee fee { get; set; }
        public payment_method payment_method { get; set; }
        public string currency { get; set; }
        public string method { get; set; }
    }
    public class fee
    {
        public double amount { get; set; }
        public double tax { get; set; }
        public string currency { get; set; }
    }
    public class payment_method
    {
        public string type { get; set; }
        public string reference { get; set; }
        public string barcode_url { get; set; }
    }
}
