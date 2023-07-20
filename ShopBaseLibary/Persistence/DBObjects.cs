﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Security.Cryptography;
using Layer3Access;
using ShopBase;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Layer3Objects
{
    public static class DBObjects
    {
        private static CultureInfo culture = new CultureInfo("en-US");
        public static void CrateDefaultDatabase()
        {
            DBAccess.CreateDefaultDataBase();

            if (DBAccess.CreateArticleTable())
            {
                TestData.InsertArticles();
            }


            if (DBAccess.CreateAdressTable())
            {
                TestData.InsertTestAdresses();
            }

            DBAccess.CreateAdressNodeTable();

            if (DBAccess.CreateCustomerTable())
            {
                TestData.InsertTestCustomers();
            }

            if (DBAccess.CreateNonCustomerTable())
            {
                TestData.InsertTestNonCustomers();
            }


            if (DBAccess.CreatePictureTable())
            {
                TestData.InsertPictures();
            }

            if (DBAccess.CreateOrderTable() && DBAccess.CreatePositionTable())
            {
                TestData.InsertOrders();
            }



        }
        public static void DeleteAllData()
        {
            string sql = "Drop Table TPosition;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TOrder;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TPicture;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TNonCustomer;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TCustomer;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TAdress;";

            DBAccess.ExecuteNonQuery(sql);

            sql = "Drop Table TArticle;";

            DBAccess.ExecuteNonQuery(sql);
        }
        public static int Insert(object item)
        {
            string? sql = null;
            int lastIndex = -1;

            if (item == null)
                throw new ArgumentException();


            switch (item)
            {

                case Customer:
                    using (SqliteConnection con = DBAccess.OpenDB())
                    {
                        Customer? c = item as Customer;

                        if (c != null && c.Adress != null)
                        {
                            using (SqliteTransaction t = con.BeginTransaction())
                            {
                                sql = $"Insert into TCustomer (EMail, Password, Firstname, Lastname, Gender) values ('{c.EMail}', '{c.Password}', '{c.Firstname}', '{c.Lastname}', '{c.Gender}');";
                                lastIndex = DBAccess.ExecuteNonQuery(sql, con, t);

                                //Insert in Node Customer/Adresse m to n relation
                                sql = $"Insert into TNodeAdress values (null, {lastIndex}, null, {c.Adress.Id}, '{c.Adress.AdressType}');";
                                DBAccess.ExecuteNonQuery(sql, con, t);

                                t.Commit();
                            }
                        }

                        sql = null;
                        break;
                    }
                case NonCustomer:
                    using (SqliteConnection con = DBAccess.OpenDB())
                    {
                        using (SqliteTransaction t = con.BeginTransaction())
                        {
                            NonCustomer n = (NonCustomer)item;
                            if (n.Adress != null)
                            {
                                sql = $"Insert into TNonCustomer values (null, '{n.EMail}', '{n.Firstname}', '{n.Lastname}', '{n.Gender}');";
                                lastIndex = DBAccess.ExecuteNonQuery(sql, con, t);
                                int idCN = lastIndex;
                                n.Id = idCN;


                                sql = $"Insert into TNodeAdress values (null, null, {idCN}, {n.Adress.Id}, '{n.Adress.AdressType}');";
                                DBAccess.ExecuteNonQuery(sql, con, t);

                                t.Commit();
                            }
                            else
                            {
                                throw new ArgumentNullException();
                            }
                        }

                        sql = null;
                        break;
                    }
                case Adress:
                    //  Insert in Node nustomer/Adresse m to n relation
                    Adress a = (Adress)item;
                    sql = $"Insert into TAdress (Street, HouseNumber, PostalCode, City, Country) values ('{a.Street}', {a.HouseNumber}, {a.PostalCode}, '{a.City}', '{a.Country}');";
                    DBAccess.ExecuteNonQuery(sql);
                    break;
                case Article:
                    sql = $"Insert into TArticle (Active, Name, Description, Price, Count) values ({((Article)item).Active}, '{((Article)item).Name}','{((Article)item).Description}',{(((Article)item).Price).ToString("#0.00", culture)}, {((Article)item).Count});";
                    lastIndex = DBAccess.ExecuteNonQuery(sql);
                    break;
                case Picture:
                    using (SqliteConnection con = DBAccess.OpenDB())
                    {
                        Picture? p = item as Picture;
                        if (p != null && p.Data != null)
                        {
                            sql = $"Insert into TPicture values (null,{(p.Article != null ? p.Article.Id : "null")},'{p.Filename}', @data, @Length);";

                            SqliteCommand ms = new(sql, con);
                            ms.Parameters.AddWithValue("@data", p.Data);
                            ms.Parameters.AddWithValue("@Length", p.Data.Length);

                            ms.ExecuteNonQuery();
                        }

                        sql = null;
                    }
                    break;
                case Order:
                    Order o = (Order)item;
                    int oId = -1;

                    if (o.Id < 1)
                    {
                        sql = $"Insert into TOrder values (null, {(o.Customer != null ? o.Customer.Id : "null")}, {(o.NonCustomer != null ? o.NonCustomer.Id : "null")}, '{((Order)item).Status}');";
                        //Whe need the Created Order Id to Insert the Positions to Database of the same connection!
                        oId = DBAccess.ExecuteNonQuery(sql);
                        o.Id = oId;
                        sql = null;
                    }
                    else
                    {
                        oId = o.Id;
                    }

                    if (o.Positions != null)
                    {
                        foreach (Position item2 in (o.Positions))
                        {
                            if (item2.Article != null)
                            {
                                if (item2.Id < 1)
                                {
                                    sql = $"Insert into TPosition values (null,{oId},{item2.Article.Id},{((Position)item2).Count})";

                                    int pid = DBAccess.ExecuteNonQuery(sql);
                                    item2.Id = pid;

                                }
                            }
                        }

                    }
                    sql = null;
                    break;
                default:
                    throw new NotImplementedException("Klasse nicht vergeben");
            }
            return lastIndex;
        }
        public static List<T> ReadAll<T>(int id = 0)
        {
            //List with diffrent Referenztypes possible and one or more
            List<object> list = new List<object>();
            object? ob = Activator.CreateInstance<T>();
            string sql = "";

            if (ob != null)
            {

                sql = $"Select * from T{ob.GetType().Name} ";

                if (id > 0)
                {
                    switch (ob)
                    {
                        case Customer:
                            sql += $"where Id = {id}";
                            break;
                        case NonCustomer:
                            sql += $"where Id = {id}";
                            break;
                        case Article:
                            sql += $"where Id = {id}";
                            break;
                        case Position:
                            sql += $"where OId = {id}";
                            break;
                        case Adress:
                            sql += $"where Id = {id}";
                            break;
                        case Picture:
                            sql += $"where AId = {id}";
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }


            sql += ";";

            using (SqliteConnection con = DBAccess.OpenDB())
            {
                using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                {
                    while (r.Read())
                    {
                        switch (ob)
                        {
                            case Customer:
                                list.Add(new Customer(r.GetInt32(0), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), (Gender)Enum.Parse(typeof(Gender), r.GetString(6)), Adress.GetFromCustomer<Customer>(r.GetInt32(0))));
                                break;
                            case NonCustomer:
                                list.Add(new NonCustomer(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), (Gender)Enum.Parse(typeof(Gender), r.GetString(4)), Adress.GetFromCustomer<NonCustomer>(r.GetInt32(0))));
                                break;
                            case Article:
                                list.Add(new Article(r.GetInt32(0), r.GetBoolean(1), r.GetString(2), r.GetString(3), r.GetDecimal(4), r.GetInt32(5)));
                                break;
                            case Position:
                                list.Add(new Position(r.GetInt32(0), Article.Get(r.GetInt32(2)), r.GetInt32(3)));
                                break;
                            case Adress:
                                list.Add(new Adress(r.GetInt32(0), r.GetString(2), r.GetInt32(3), r.GetInt32(4), r.GetString(5), r.GetString(6)));
                                break;
                            case Picture:
                                Article? a = null;
                                try
                                {
                                    if (id > 0)
                                    {
                                        a = Article.Get(r.GetInt32(1));
                                    }
                                    int pid = r.GetInt32(0);
                                    string title = r.GetString(2);
                                    int l = r.GetInt32(4);
                                    byte[] ba = new byte[l];
                                    int columCount = r.GetOrdinal("Data");
                                    r.GetBytes(columCount, 0, ba, 0, l); //No methode known for get the byte array


                                    list.Add(new Picture(pid, title, ba));
                                }
                                catch (Exception)
                                {

                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }
            return list.Cast<T>().ToList();
        }
        public static List<Order> GetAllOrderFromCustomer(int cid)
        {
            List<Order> list = new List<Order>();
            string sql = $"Select * From TOrder where Cid = {cid};";

            using (SqliteConnection con = DBAccess.OpenDB())
            {
                using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                {
                    while (r.Read())
                    {
                        list.Add(new Order(r.GetInt32(0), Customer.Get(r.GetInt32(1)), (Status)Enum.Parse(typeof(Status), r.GetString(3)), Position.GetAll(r.GetInt32(0))));
                    }
                }
            }

            return list;
        }
        public static List<Order> GetAllOrderFromNonCustomer(int cid)
        {
            List<Order> list = new List<Order>();
            string sql = $"Select * From TOrder where CNid = {cid}";

            using (SqliteConnection con = DBAccess.OpenDB())
            {
                using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                {
                    while (r.Read())
                    {
                        list.Add(new Order(r.GetInt32(0), NonCustomer.Get(r.GetInt32(2)), (Status)Enum.Parse(typeof(Status), r.GetString(3)), Position.GetAll(r.GetInt32(0))));
                    }
                }
            }

            return list;
        }
        public static void Change<T>(object item)
        {
            string? sql = $"Update T{typeof(T).Name} " +
                         $"Set ";


            using (SqliteConnection con = DBAccess.OpenDB())
            {
                switch (item)
                {
                    case Customer:
                        Customer c = (Customer)item;
                        sql += $"Firstname = '{c.Firstname}', Lastname = '{c.Lastname}', EMail = '{c.EMail}'";
                        sql += c.Password != null ? $", Password = '{c.Password}' " : " ";
                        sql += $"where Id = {c.Id};";
                        if (c.Id == 1)
                        {
                            throw new InvalidOperationException("Customer Tina Test can not be deleted");
                        }
                        break;
                    case Article:
                        Article a = (Article)item;

                        sql += $"Name = '{a.Name}', Description = '{a.Description}', Price = {a.Price.ToString("#0.00", culture)}, Count = {a.Count} Where Id = {a.Id}";

                        break;
                    case Order: // Only the Status chan be Changed 
                        Order o = (Order)item;
                        if (o.Status == Status.Bestellt)
                        {
                            using (SqliteTransaction t = con.BeginTransaction())
                            {
                                sql += $"Status = '{o.Status}' where Id = {o.Id};";

                                DBAccess.ExecuteNonQuery(sql, con, t);

                                if (o.Positions != null)
                                {
                                    o.Positions.ForEach(p =>
                                    {
                                        if (p.Article != null)
                                        {
                                            p.Article.Count -= p.Count;
                                            sql = $"Update TArticle Set Count = {p.Article.Count} where Id = {p.Article.Id}";
                                            DBAccess.ExecuteNonQuery(sql, con, t);
                                        }

                                    });

                                }

                                t.Commit();
                                sql = null;

                            }


                        }
                        else
                        {
                            using (SqliteTransaction t = con.BeginTransaction())
                            {
                                sql += $"Status = '{o.Status}' where Id = {((Order)item).Id};";

                                DBAccess.ExecuteNonQuery(sql, con, t);

                                sql = "";
                                
                                if (o.Status == Status.Warenkorb && o.Positions != null)
                                {
                                    o.Positions.ForEach(p =>
                                    {
                                        sql += $"Update TPosition Set Count = {p.Count} where Id = {p.Id};";
                                        DBAccess.ExecuteNonQuery(sql, con, t);
                                        sql = "";
                                    });

                                }

                                t.Commit();
                                sql = null;

                            }
                        }
                        break;
                    case Picture:
                        Picture p = (Picture)item;
                        if (p.Article != null && p.Data != null)
                        {
                            sql += $"Filename = '{p.Filename}',Data = @Data,Length = @Length ";
                            sql += $"Where AId = {p.Article.Id};";

                            SqliteCommand ms = new(sql, con);
                            ms.Parameters.AddWithValue("@Data", p.Data);
                            ms.Parameters.AddWithValue("@Length", p.Data.Length);
                            ms.ExecuteNonQuery();
                        }

                        sql = null;
                        break;
                    default:
                        break;

                }

                DBAccess.ExecuteNonQuery(sql, con);
            }

        }
        public static void Delete<T>(object item)
        {
            string sql = $"Delete From T{typeof(T).Name} ";


            using (SqliteConnection con = DBAccess.OpenDB())
            {
                switch (item)
                {
                    case Customer:
                        Customer c = (Customer)item;
                        sql += $"Where Id = {c.Id}"; // Where CId = x
                        if (c.Id == 1)
                        {
                            throw new InvalidOperationException("Customer Tina Test can not be deleted");
                        }
                        break;
                    case Article:
                        Article a = (Article)item; // Because No Article can be deleted because it could be related to Order
                        sql = "Update TArticle Set Active = false ";
                        sql += $"Where Id = {a.Id}";
                        break;
                    case Order:
                        Order o = (Order)item;
                        sql += o.Status == Status.Warenkorb ? $"Where Id = {o.Id}" : throw new Exception($"Delete of Order Id {o.Id} not possible because Status is not basket");
                        break;
                    case Position:
                        Position p = (Position)item;
                        sql += $"where Id = {p.Id};";
                        break;
                    default:
                        break;
                }

                DBAccess.ExecuteNonQuery(sql);
            }
        }
        public static int ReadCustomer(string email, string pwd)
        {
            int ret = -1; //Email is unique, lowest customer number is 1
            string sql = $"SELECT Id FROM TCustomer WHERE EMail = @un and Password = @pw";


            using (SqliteConnection con = DBAccess.OpenDB())
            {
                SqliteCommand cmd = new(sql, con);
                cmd.Parameters.AddWithValue("@un", email);
                cmd.Parameters.AddWithValue("@pw", pwd);

                using (SqliteDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        ret = r.GetInt32(0);
                    }
                }
            }

            return ret;
        }
        public static int GetAdressID<T>(int id)
        {
            string sql = "";
            int aid = 0;

            if (typeof(T) == typeof(Customer))
            {
                sql = $"Select AId " +
                      $"From TNodeAdress " +
                      $"Where CId = {id};";
            }
            else if (typeof(T) == typeof(NonCustomer))
            {
                sql = $"Select AId " +
                      $"From TNodeAdress " +
                      $"Where CNId = {id};";
            }

            using (SqliteConnection con = DBAccess.OpenDB())
            {
                using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                {
                    r.Read();

                    aid = r.GetInt32(0);
                }
            }

            return aid;
        }
        public static Adress GetAdressWithoutID(Adress a)
        {
            string sql = "";
            int aid = -1;

            if (a.Street != null && a.HouseNumber > 0 && a.PostalCode > 0 && a.City != null && a.Country != null)
            {

                sql = "Select Id " +
                      "From TAdress " +
                      $"Where Street = '{(a.Street != null ? a.Street.Trim() : "")}' and HouseNumber = {a.HouseNumber} and PostalCode = {a.PostalCode} and City = '{a.City.Trim()}' and Country = '{a.Country}';";
            }
            using (SqliteConnection con = DBAccess.OpenDB())
            {
                using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                {
                    r.Read();

                    try
                    {
                        aid = r.GetInt32(0);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // Declare Local function that saves the given Adress which does not exists and returns it again
            Func<Adress, Adress> InsertReturn = (Adress a) => { a.Insert(); return a; };

            // Rekursive call if Adress not exists and after inserting with local funtion, because it than has id
            return aid > 0 ? Adress.Get(aid) : GetAdressWithoutID(InsertReturn(a));
        }
        public static Order? MergeBasket(Order o)
        {
            if (o == null || o.Positions == null)
            {
                return o;
            }

            Customer? c = o.Customer;
            NonCustomer? n = o.NonCustomer;
            Order? mergedBasket = null;
            List<Order> list = new List<Order>();


            foreach (Position item in o.Positions)
            {
                if (item != null)
                {
                    if (n != null)
                        mergedBasket ??= new(n, Status.Warenkorb, new List<Position>());
                    else
                        mergedBasket ??= new Order(o.Id, c, Status.Warenkorb, new List<Position>());


                    if (item.Article != null && mergedBasket != null && mergedBasket.Positions != null && mergedBasket.Positions.Contains<Position>(item) == false)
                    {
                        item.Count = item.Count > item.Article.Count ? item.Article.Count : item.Count;


                        mergedBasket.Positions.Add(item);
                    }
                    else
                    {
                        if (mergedBasket != null && mergedBasket.Positions != null && item.Article != null)
                        {
                            Position? pO = mergedBasket.Positions.Find(x => x.Article != null ? x.Article.Id == item.Article.Id : false);

                            if (pO != null && pO.Article != null)
                                pO.Count = pO.Count + item.Count > pO.Article.Count ? pO.Article.Count : pO.Count + item.Count;
                        }

                    }
                }
            }

            //Delete Zero Positions

            if (mergedBasket != null && mergedBasket.Positions != null)
                mergedBasket.Positions = mergedBasket.Positions.FindAll(p => p.Count > 0);

            // Update Basket
            if (mergedBasket != null && mergedBasket.Customer != null)
            {
                mergedBasket.Change();

                List<Position>? pl = null;
                List<Order>? ol = Order.GetAllFromCustomer(mergedBasket.Customer.Id);

                if (ol != null)
                {
                    Order? o2 = ol.Find(x => x.Status == Status.Warenkorb);

                    if (o2 != null && o2.Positions != null)
                        pl = o2.Positions;

                }

                if (pl != null)
                {

                    if (mergedBasket != null && mergedBasket.Positions != null)
                    {
                        mergedBasket.Positions.ForEach(x => pl.Remove(x));
                    }

                    pl.ForEach(x => mergedBasket.DeletePosition(x));
                }




            }



            return mergedBasket;
        }
        public static Picture? GetFromArticle(Article a)
        {
            Picture? p = new Picture();

            try
            {
                string sql = "Select Id " +
                             "From TPicture " +
                             $"Where AId = {a.Id};";

                using (SqliteConnection con = DBAccess.OpenDB())
                {
                    using (SqliteDataReader r = DBAccess.ExecuteReader(sql, con))
                    {
                        r.Read();

                        p = ReadAll<Picture>(a.Id)[0];

                        p.Article = a;
                    }
                }
            }
            catch (Exception)
            {
                p = null;
            }

            return p;
        }
    }
}
