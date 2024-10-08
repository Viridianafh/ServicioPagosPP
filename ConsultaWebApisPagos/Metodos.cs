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
        public static string RegresarBoleto(string internet_sale_id)
        {
            Logs.crealogs(DateTime.Now.ToString() + " Iniciando proceso de devolucion a trip_seat.");
            string query = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{internet_sale_id}'";
            DataTable tbl3 = new DataTable();
            tbl3 = BaseDatos.MandaQuerySelect(query);
            if (tbl3.Rows.Count > 0)
            {
                string ticket = GenShortId();
                query = $@"insert into trip_seat(id,version,date_created,last_updated,seat_id,status,starting_stop_id,ending_stop_id,
					internet_sale_id,trip_id,seat_name,passenger_name,ticket_id,comments,passenger_type,user_id,sold_price,
					payed_price,original_price)
                    select id,version,date_created,last_updated,seat_id,'OCCUPIED',starting_stop_id,ending_stop_id,
					internet_sale_id,trip_id,seat_name,passenger_name,'{ticket}','Regreso validacion 2',passenger_type,user_id,sold_price,
					payed_price,sold_price  from cancel_reservation where internet_sale_id='{internet_sale_id}' LIMIT 1";
                BaseDatos.MandaQueryInsertDeleteUpdate(query);

                query = $"UPDATE internet_sale SET source_meta='Validado y envio de correo', payed=true WHERE id='{internet_sale_id}'";
                BaseDatos.MandaQueryInsertDeleteUpdate(query);
                Logs.crealogs(DateTime.Now.ToString() + " Fin proceso de devolucion a trip_seat correctamente.");
                return ticket;
            }
            else
            {
                Logs.crealogs(DateTime.Now.ToString() + " No se encontraron coincidencias en cancel_resercation.");
                return "";
            }

        }
        public static string GenShortId()
        {
            var sh = "";
            var characters = "ab0cde1fghi2jkl3mno4pqrs5tuvw6xyz7ABC8DEF9GHIJKLMNOPQRSTUVWXYZ";
            var Charsarr = new char[8];
            var random = new Random();
            for (int i = 0; i < Charsarr.Length; i++)
            {
                sh = sh + characters[random.Next(characters.Length)];
            }
            return sh;
        }
        public static string GenerarBoleto(string internet_sale_id)
        {
            string res = "";
            DateTime fecha = DateTime.Now;
            int tick = 0, cont = 0;
            string GetBol = "SELECT * FROM trip_seat WHERE internet_sale_id='" + internet_sale_id + "'";
            DataTable tbl2 = new DataTable();
            tbl2 = BaseDatos.MandaQuerySelect(GetBol);
            string ticketid = GenShortId();
            if (tbl2.Rows.Count == 0)
            {
                Logs.crealogs(DateTime.Now.ToString() + " Inconsistencia, no existen registros en trip_seat.");
                return RegresarBoleto(internet_sale_id);
            }
            else
            {
                foreach (DataRow row3 in tbl2.Rows)
                {
                    while (cont == 0)
                    {
                        string qry21 = "SELECT * FROM trip_seat WHERE ticket_id='" + ticketid + "'";
                        DataTable tbl3 = new DataTable();
                        tbl3 = BaseDatos.MandaQuerySelect(qry21);
                        if (tbl3.Rows.Count == 0)
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " ticket_id asignado: " + ticketid);
                            cont++;
                        }
                        else
                        {
                            ticketid = GenShortId();
                        }
                    }
                    try
                    {
                        string qry2 = "UPDATE trip_seat set status='OCCUPIED',ticket_id='" + ticketid + "',version=1 WHERE id='" + row3["id"] + "'";
                        string respuesta = BaseDatos.MandaQueryInsertDeleteUpdate(qry2);
                        if (respuesta == "Correcto")
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " Boleto creado correctamente: " + ticketid);
                            res = ticketid;
                        }
                        else
                        {
                            Logs.crealogs(DateTime.Now.ToString() + " No se ha creado el boleto.");
                            res = "";
                        }
                    }
                    catch { }
                    ticketid = GenShortId();
                }
                try
                {
                    string qry3 = "UPDATE internet_sale set version=2,last_updated='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "',payed='true' WHERE id='" + internet_sale_id + "';";
                    string respuesta = BaseDatos.MandaQueryInsertDeleteUpdate(qry3);
                    if (respuesta == "Correcto")
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Datos actualizados en internet_sale.");
                    }
                    else
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Fallo al momento de actualizar internet_sale.");
                    }
                }
                catch { }
            }
            return res;
        }
        public static string ObtenerLugar(string shorId, string internetSaleId, string tipo)
        {
            string descripcion = "";
            if (tipo == "destino")
            {
                DataTable tbl = new DataTable();
                string sqry = "select so.name from trip_seat ts inner join stop_off so on ts.ending_stop_id = so.id where ts.internet_sale_id like '" + internetSaleId + "'";
                tbl = BaseDatos.MandaQuerySelect(sqry);
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
                tbl = BaseDatos.MandaQuerySelect(sqry);
                foreach (DataRow row in tbl.Rows)
                {
                    descripcion = Convert.ToString(row["name"]);
                }
            }
            return descripcion;
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
            tbl = BaseDatos.MandaQuerySelect(sqry);
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
            tbl2 = BaseDatos.MandaQuerySelect(sqry2);
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

        public static void pagoconfirmado()
        {

        }
        #region Paypal
        public static bool BuscarPagosPayPal(string starting_day, string ending_day, string op)
        {
            Console.WriteLine("################----PayPal----#######################");
            Logs.crealogs("\n" + DateTime.Now.ToString() + " ################----PayPal----#######################");
            
            string correo_envio = "", short_id = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime hora_actual = DateTime.Now;
            string concepto = "";
            Console.WriteLine("Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            Logs.crealogs(DateTime.Now.ToString() + " Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            //Console.WriteLine(hora_actual);
            bool verificacion = false;
            DataTable tbl = new DataTable();
            bool Result = true;
            Console.WriteLine("Inicio: " + DateTime.Now.ToString());

            string sqry = "SELECT * FROM internet_sale WHERE (payment_provider LIKE '%ay%al%' OR payment_provider LIKE '%Gateway') AND date_created >= CURRENT_DATE - INTERVAL '2 hour' ORDER BY date_created DESC";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%ay%al%' OR payment_provider LIKE '%Gateway' ORDER BY date_created DESC LIMIT 1";
            //string sqry = "SELECT * FROM internet_sale WHERE short_id='926108a7'";
            try
            {
                tbl = BaseDatos.MandaQuerySelect(sqry);
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
                    Logs.crealogs("\n");
                    Logs.crealogs(DateTime.Now.ToString() + " Proceso para short_id: " + row["short_id"].ToString());
                    Logs.crealogs(DateTime.Now.ToString() + " Status internet_sale:" + row["payed"].ToString());
                    if (Convert.ToString(row["full_response"]).Contains("paypal") && Convert.ToString(row["full_response"]).Contains("token"))
                    {
                        string[] partes1 = Convert.ToString(row["full_response"]).Split('?');
                        string info = partes1[1];
                        string[] partes2 = info.Split(',');
                        string tokens = partes2[0];
                        string token = tokens.Split('&')[0].Split('=')[1];
                        string acces_token = tokens.Split('&')[1].Split('=')[1];
                        acces_token = acces_token.Remove(acces_token.Length - 1);
                        string amount = partes2[2].Split(':')[1];
                        amount = amount.Remove(amount.Length - 1) + ".00";

                        //verificacion = VerificarPayPal(Convert.ToString(row["full_response"]), Convert.ToString(row["total_amount"]), Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                        verificacion = ConfirmarPago(acces_token, token, amount, Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                        
                        if (verificacion)
                        {
                            try
                            {
                                if (row["source_meta"].ToString() == "Validado y envio de correo" || row["source_meta"].ToString().Contains("Envio de correo"))
                                {
                                    Logs.crealogs(DateTime.Now.ToString() + " Verificacion correcta, verificando integridad de los datos.");
                                    string query = $"SELECT * FROM trip_seat WHERE internet_sale_id = '{Convert.ToString(row["id"])}'";
                                    DataTable dt = BaseDatos.MandaQuerySelect(query);
                                    if (dt.Rows.Count == 0)
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Fallo de integridad.");
                                        RegresarBoleto(Convert.ToString(row["id"]));
                                    }
                                    else
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Integridad correcta.");
                                    }
                                }
                                else
                                {

                                    Logs.crealogs(DateTime.Now.ToString() + " Verificacion correcta inicio creacion de boleto.");
                                    string ticket = "";
                                    while (ticket == "")
                                    {
                                        ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                    }
                                    if (ticket != "")
                                    {
                                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                        var temp_fechas = fechas.Split(',');
                                        fecha_salida = temp_fechas[0];
                                        fecha_llegada = temp_fechas[1];

                                        try
                                        {
                                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                            concepto = "Validado y envio de correo";
                                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                            string respuesta = BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                            if (respuesta == "Correcto")
                                            {
                                                Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + row["email"]);
                                            }
                                            else
                                            {
                                                Logs.crealogs(DateTime.Now.ToString() + " Error actualizacion trip_seat, No se envio correo a :" + row["email"]);
                                            }

                                        }
                                        catch
                                        {
                                            concepto = "Error al enviar el correo";
                                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                            BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                            Logs.crealogs(DateTime.Now.ToString() + " Error al enviar el correo: " + row["email"]);
                                        }
                                    }                                    
                                }
                            }
                            catch (Exception ex)
                            {
                                Logs.crealogs(DateTime.Now.ToString() + " Error no se continuara con el proceso, razon: " + ex.Message);
                            }
                        }
                        else
                        {
                            if (!(row["source_meta"].ToString() == "Validado y envio de correo" || row["source_meta"].ToString().Contains("Envio de correo")))
                            {
                                verificacion = VerificarPayPal(Convert.ToString(row["full_response"]), Convert.ToString(row["total_amount"]), Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                                if (verificacion)
                                {
                                    Logs.crealogs(DateTime.Now.ToString() + " Verificacion correcta inicio creacion de boleto.");
                                    string ticket = "";
                                    while (ticket == "")
                                    {
                                        ticket = GenerarBoleto(Convert.ToString(row["id"]));
                                    }
                                    if (ticket != "")
                                    {
                                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                        var temp_fechas = fechas.Split(',');
                                        fecha_salida = temp_fechas[0];
                                        fecha_llegada = temp_fechas[1];

                                        try
                                        {
                                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), ticket, Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                            concepto = "Validado y envio de correo";
                                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                            string respuesta = BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                            if (respuesta == "Correcto")
                                            {
                                                Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + row["email"]);
                                            }
                                            else
                                            {
                                                Logs.crealogs(DateTime.Now.ToString() + " Error actualizacion trip_seat, No se envio correo a :" + row["email"]);
                                            }

                                        }
                                        catch
                                        {
                                            concepto = "Error al enviar el correo";
                                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                            BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                            Logs.crealogs(DateTime.Now.ToString() + " Error al enviar el correo: " + row["email"]);
                                        }
                                    }                                    
                                }
                                else
                                {
                                    Logs.crealogs(DateTime.Now.ToString() + " Verificacion no exitosa.");
                                    if (row["source_meta"].ToString() == "No se valido el boleto en 1 hr")
                                    {
                                        Logs.crealogs(DateTime.Now.ToString() + " Proceso de liberacion ya realizado.");
                                    }
                                    else
                                    {
                                        DateTime fechaHora = DateTime.Parse(row["date_created"].ToString());
                                        //fechaHora = fechaHora.AddHours(-5);
                                        fechaHora = fechaHora.AddHours(1);
                                        if (DateTime.Now > fechaHora)
                                        {
                                            Logs.crealogs(DateTime.Now.ToString() + " Expiro el tiempo de espera, inicia liberacion de asientos.");
                                            string sqryf = "UPDATE internet_sale SET source_meta='No se valido el boleto en 1 hr' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                            BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                            try
                                            {
                                                sqryf = $"SELECT * FROM trip_seat WHERE internet_sale_id='{Convert.ToString(row["id"])}'";
                                                DataTable tscr = BaseDatos.MandaQuerySelect(sqryf);
                                                if (tscr.Rows.Count > 0)
                                                {
                                                    Console.WriteLine();
                                                    sqryf = $"insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status, F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id, f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name, 'Serv_Liberar_Asiento_PayPal',f.trip_id,f.version,f.date_created, CURRENT_TIMESTAMP from trip_seat f where f.internet_sale_id = '{row["id"].ToString()}'";
                                                    BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                                    Console.WriteLine();
                                                    sqryf = $"DELETE FROM trip_seat WHERE internet_sale_id = '{row["id"].ToString()}'";
                                                    BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                                    Console.WriteLine();
                                                }
                                                else
                                                {
                                                    sqryf = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{Convert.ToString(row["id"])}' AND comments='Serv_Liberar_Asiento_PayPal'";
                                                    tscr = BaseDatos.MandaQuerySelect(sqryf);
                                                    if (tscr.Rows.Count > 0)
                                                    {
                                                        sqryf = "UPDATE cancel_reservation SET comments='Serv_Liberar_Asiento_MasterCard' WHERE internet_sale_id='" + Convert.ToString(row["id"]) + "'";
                                                        BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                                        Console.WriteLine();
                                                    }
                                                }

                                            }
                                            catch (Exception e)
                                            {
                                                Logs.crealogs(DateTime.Now.ToString() + " " + e.Message);
                                            }
                                            Logs.crealogs(DateTime.Now.ToString() + " Finalizo la liberacion de asientos.");
                                        }
                                        else
                                        {
                                            Logs.crealogs(DateTime.Now.ToString() + " En espera de recibir el pago.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logs.crealogs(DateTime.Now.ToString() + " Fin proceso para short_id: " + row["short_id"].ToString() + " Boleto ya creado.");
                            }                                                          
                        }
                        Logs.crealogs(DateTime.Now.ToString() + " Fin proceso para short_id: " + row["short_id"].ToString());
                    }
                    else
                    {
                        Logs.crealogs(DateTime.Now.ToString() + " Fin proceso para short_id: " + row["short_id"].ToString() + " No procesable.");
                    }
                    
                    /*if (Convert.ToString(row["payed"]) == "False")
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
                                    BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Validado y envio de correo: " + row["email"].ToString());
                                }
                                catch
                                {
                                    concepto = "Error al enviar el correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al enviar el correo: " + row["email"].ToString());
                                }
                            }
                            catch
                            {
                                if (op == "1")
                                {
                                    concepto = "Error al generar ticket para: " + short_id.ToString();
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                    Console.WriteLine("Error al generar ticket para: " + short_id);
                                    EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    EnvioMail.EnviaCorreo("alejandro.horta@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    EnvioMail.EnviaCorreo("viridiana.fh@transportesmedrano.com", "", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                    Logs.crealogs(DateTime.Now.ToString() + " Error al generar ticket para: " + row["short_id"].ToString() + "Envio correo a luis.rojas@transportesmedrano.com viridiana.fh@transportesmedrano.com alejandro.horta@transportesmedrano.com");
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
                                BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                
                                try
                                {
                                    sqryf = $"SELECT * FROM trip_seat WHERE internet_sale_id='{Convert.ToString(row["id"])}'";
                                    DataTable tscr = BaseDatos.MandaQuerySelect(sqryf);
                                    if (tscr.Rows.Count > 0)
                                    {
                                        Console.WriteLine();
                                        sqryf = $"insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status, F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id, f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name, 'Serv_Liberar_Asiento_245',f.trip_id,f.version,f.date_created, CURRENT_TIMESTAMP from trip_seat f where f.internet_sale_id = '{row["id"].ToString()}'";
                                        BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                        Console.WriteLine();
                                        sqryf = $"DELETE FROM trip_seat WHERE internet_sale_id = '{row["id"].ToString()}'";
                                        BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                                        Console.WriteLine();
                                    }
                                    else
                                    {
                                        sqryf = $"SELECT * FROM cancel_reservation WHERE internet_sale_id='{Convert.ToString(row["id"])}' AND comments='Serv_Liberar_Asiento'";
                                        tscr = BaseDatos.MandaQuerySelect(sqryf);
                                        if (tscr.Rows.Count > 0)
                                        {
                                            sqryf = "UPDATE cancel_reservation SET comments='Serv_Liberar_Asiento_245' WHERE internet_sale_id='" + Convert.ToString(row["id"]) + "'";
                                            BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
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

                        DataTable t = BaseDatos.MandaQuerySelect($"SELECT t2.ticket_id FROM trip_seat t2 INNER JOIN internet_sale t1 ON t1.id=t2.internet_sale_id WHERE t1.id='{Convert.ToString(row["id"])}' LIMIT 1");
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
                            BaseDatos.MandaQueryInsertDeleteUpdate(sqryf);
                            Logs.crealogs(DateTime.Now.ToString() + " Exito solo se enviara el correo: " + row["email"].ToString() + " para: " + row["short_id"].ToString());
                        }
                    }*/
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
                //var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/CapturaPaypal?id=" + acces_token + "&access_token=" + token + "&boletos=1&monto=" + total);
                var requestToken = (HttpWebRequest)WebRequest.Create("http://admin.sagautobuses.com:403/Home/CapturaPaypal?id=" + acces_token + "&access_token=" + token + "&boletos=1&monto=" + total);
                //var requestToken = (HttpWebRequest)WebRequest.Create("https://localhost:44329/Home/CapturaPaypal?id=" + acces_token + "&access_token=" + token + "&boletos=1&monto=" + total);
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
                                res = ConfirmarPago(token, acces_token, total, shortId, internet_sale_id);
                                /*Console.WriteLine(responseBody);
                                if (responseBody != "")
                                    res = true;
                                else
                                    res = false;*/
                                return res;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al capturar pago");
                    Logs.crealogs(DateTime.Now.ToString() + " Error al capturar pago");
                    res = false;
                    return res;
                }                
            }
            else
            {
                res = false;
                return res;
            }                       
        }
        public static bool ConfirmarPago(string access_token, string token, string amount, string shortId, string internet_sale_id)
        {
            string responseBody = null;
            //var requestToken = (HttpWebRequest)WebRequest.Create("http://transmed.dyndns-web.com:400/Home/RevisarPaypal?id=" + token + "&access_token=" + access_token);
            var requestToken = (HttpWebRequest)WebRequest.Create("http://admin.sagautobuses.com:403/Home/RevisarPaypal?id=" + token + "&access_token=" + access_token);
            //var requestToken = (HttpWebRequest)WebRequest.Create("https://localhost:44329/Home/RevisarPaypal?id=" + token + "&access_token=" + access_token);
            requestToken.ContentType = "application/json";
            requestToken.Method = "GET";
            requestToken.ContentLength = 0;
            try
            {
                using (WebResponse responseToken = requestToken.GetResponse())
                {
                    using (Stream strReader = responseToken.GetResponseStream())
                    {
                        if (strReader == null) return false;
                        using (StreamReader objReader = new StreamReader(strReader))
                        {
                            responseBody = objReader.ReadToEnd();
                            responseBody = "[" + responseBody + "]";
                            var rs = JsonConvert.DeserializeObject<List<RevPayPal>>(responseBody);
                            if (Convert.ToString(rs[0].status) == "COMPLETED")
                            {
                                //responseBody = CapturarPayPal(shortId, internet_sale_id, amount);
                                Logs.crealogs(DateTime.Now.ToString() + " status pago: COMPLETED");
                                return true;
                            }
                            else
                            {
                                responseBody = "";                                
                                Logs.crealogs(DateTime.Now.ToString() + " status pago: " + rs[0].status.ToString());
                                return false;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                responseBody = "";
                return false;
            }
        }
        public static string CapturarPayPal(string short_id, string internet_sale_id, string amount)
        {
            string responseBody = null;
            //var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/UpdateBoletoMC?shortId=" + short_id + "&sale_id=" + internet_sale_id + "&amount=" + amount);
            var requestToken = (HttpWebRequest)WebRequest.Create("http://admin.sagautobuses.com:403/Home/UpdateBoletoMC?shortId=" + short_id + "&sale_id=" + internet_sale_id + "&amount=" + amount);
            //var requestToken = (HttpWebRequest)WebRequest.Create("https://localhost:44329/Home/UpdateBoletoMC?shortId=" + short_id + "&sale_id=" + internet_sale_id + "&amount=" + amount);
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
