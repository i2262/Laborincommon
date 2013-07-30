/****************************** Module Header ******************************\
* Module Name:  GenerateDeepZoomService.cs
* Project:      DeepZoomProjectSite
* Copyright (c) Microsoft Corporation.
* 
* This example demonstrates how to generate the deep zoom content programmatically in Silverlight using C#. It wraps the functionality in a WCF service.
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
* 
* History:
* * 8/27/2009 15:40 Yilun Luo Created
\***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.ServiceModel.Activation;
using System.Web;
using System.Net;
using Microsoft.DeepZoomTools;
using System.Xml.Linq;
using System.Windows;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;


// NOTE: If you change the class name "GenerateDeepZoomService" here, you must also update the reference to "GenerateDeepZoomService" in Web.config.
[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
public class GenerateDeepZoomService : IGenerateDeepZoomService
{
    double Article_Height;
    double Article_Width;


	public bool PrepareDeepZoom(bool forceGenerateDeepZoom)
	{


        string Connect = "Database=my_site_db689;Data Source=.\\SQLExpress;User Id=joomlauser689;Password=lD[&7[odyU|t";
        //Переменная Connect - это строка подключения в которой:
        //БАЗА - Имя базы в Sql
        //ХОСТ - Имя или IP-адрес сервера (если локально то можно и localhost)
        //ПОЛЬЗОВАТЕЛЬ - Имя пользователя Sql
        //ПАРОЛЬ - говорит само за себя - пароль пользователя БД Sql
        using ( SqlConnection myConnection = new SqlConnection(Connect))
        {
            myConnection.Open(); //Устанавливаем соединение с базой данных.
            string query;
            int Count;
            SqlCommand myCommand;
       
            // Проверим есть ли новые пользователи - создание аватарки
            query = "SELECT Count(*) FROM jos_users WHERE newUser=0;";
            try
            {
                myCommand = new SqlCommand(query, myConnection);

                Count = int.Parse(myCommand.ExecuteScalar() + "");
            }
            catch (Exception ex)
            {
                return false;
            }

            if (Count != 0)
            {
                forceGenerateDeepZoom = true;
                this.NewUserAvatar(myConnection);
            }
            // Проверим есть ли изменение аватара пользователя - изменение аватарки
            query = "SELECT Count(*) FROM jos_users WHERE newUser < 0;";
            myCommand = new SqlCommand(query, myConnection);
            Count = int.Parse(myCommand.ExecuteScalar() + "");
            if (Count != 0)
            {
                forceGenerateDeepZoom = true;
                this.UserAvatarChange(myConnection);
            }


            // Проверим есть ли новые ситатьи     `new`=0 ПРИЗНАК НОВОЙ   
            query = "SELECT Count(*) FROM jos_content WHERE new = 0;";
            myCommand = new SqlCommand(query, myConnection);
            Count = int.Parse(myCommand.ExecuteScalar() + "");
            if (Count != 0)
            {
                forceGenerateDeepZoom = true;
                NewArticle(myConnection);

            }//if (Count != 0)
        }
       // myConnection.Close(); //Обязательно закрываем соединение!



		if(!Directory.Exists(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/dzc_output_images")) || forceGenerateDeepZoom)
		{
			try
			{
				this.CreateDeepZoom();
			}
			catch
			{
				return false;
			}
		}
		return true;
	}


    /// <summary>
    /// Есть новая статья создаем картинку
    /// </summary>
    private void NewArticle(SqlConnection myConnection) // Есть новая статья создаем картинку
    {
        // user_id 
        //        query = "SELECT id FROM `my_site_db689`.`jos_users` WHERE `newUser`=0;";
        //      myCommand = new SqlCommand(query, myConnection);
        //    znachenie = myCommand.ExecuteScalar().ToString();
        //  string user_id = znachenie;

        string znachenie;
        string query;
        double x;
        double y;
        double Width;
        double Height;
        int NumberOfPictures;
        XDocument doc;
        SqlCommand myCommand;

        //Если запрос нам возвращает одно значение (надо быть уверенным что запрос вернёт именно одно значение иначе будет ошибка)./////???

        // запросим номер пользователя создающего статью
        query = "SELECT created_by FROM jos_content WHERE new = 0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        int created_by = Convert.ToInt32(znachenie);


        // запросим номер родительской категории новой (созданной при создании статьи)
        query = "SELECT id FROM jos_content WHERE new = 0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        int id = Convert.ToInt32(znachenie);

        if (id == 0)// id = 0 после создания каталога СТАТЬИ невозможно! 
        {
            //получим значения из профиля пользователя  - для значений по умолчанию  (C:\0\CSSL3DeepZoom\CSSL3DeepZoom.Web\components\com_users\controllers\registration.php)
            query = "SELECT x FROM jos_users WHERE id=@param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            znachenie = myCommand.ExecuteScalar().ToString();
            x = Convert.ToDouble(znachenie);

            query = "SELECT y FROM jos_users` WHERE id=@param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            znachenie = myCommand.ExecuteScalar().ToString();
            y = Convert.ToDouble(znachenie);

            query = "SELECT Width FROM jos_users WHERE id=@param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            znachenie = myCommand.ExecuteScalar().ToString();
            Width = Convert.ToDouble(znachenie);

            query = "SELECT Height FROM jos_users WHERE id=@param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            znachenie = myCommand.ExecuteScalar().ToString();
            Height = Convert.ToDouble(znachenie);

            query = "SELECT NumberOfPictures FROM jos_users` WHERE id=@param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            znachenie = myCommand.ExecuteScalar().ToString();
            NumberOfPictures = Convert.ToInt32(znachenie);

            //запись данных категории @param_NumberOfPictures+1
            query = "UPDATE jos_users SET NumberOfPictures=@param_NumberOfPictures+1 WHERE id = @param_created_by;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_NumberOfPictures", NumberOfPictures);
            myCommand.Parameters.AddWithValue("@param_created_by", created_by);
            myCommand.ExecuteNonQuery();


        }
        else
        {
            // получим id от картинки родительской статьи ВНИМАНИЕ В parent_id   ТОЛЬКО oldArtId !!!


            query = "SELECT old_Art_Id FROM jos_content WHERE id = @param_id;";   // oldArtId
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_id", id);
            znachenie = myCommand.ExecuteScalar().ToString();
            int old_Art_Id = Convert.ToInt32(znachenie);

            if (old_Art_Id == 0)// oldArtId = 0  создаем статью и ее категорию в аватарке пользователя 
            {
                //получим значения из профиля пользователя  - для значений по умолчанию ???? создана но не проверена и не используется категормя для пользователя (C:\0\CSSL3DeepZoom\CSSL3DeepZoom.Web\components\com_users\controllers\registration.php)
                // ???? нужна ли пользователю категория ???

                query = "SELECT x FROM jos_users WHERE id=@param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                znachenie = myCommand.ExecuteScalar().ToString();
                x = Convert.ToDouble(znachenie);

                query = "SELECT y FROM jos_users WHERE id=@param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                znachenie = myCommand.ExecuteScalar().ToString();
                y = Convert.ToDouble(znachenie);

                query = "SELECT Width FROM jos_users WHERE id=@param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                znachenie = myCommand.ExecuteScalar().ToString();
                Width = Convert.ToDouble(znachenie);

                query = "SELECT Height FROM jos_users WHERE id=@param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                znachenie = myCommand.ExecuteScalar().ToString();
                Height = Convert.ToDouble(znachenie);

                query = "SELECT NumberOfPictures FROM jos_users WHERE id=@param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                znachenie = myCommand.ExecuteScalar().ToString();
                NumberOfPictures = Convert.ToInt32(znachenie);

                //запись данных категории @param_NumberOfPictures+1
                query = "UPDATE jos_users` SET `NumberOfPictures`=@param_NumberOfPictures+1 WHERE id = @param_created_by;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_NumberOfPictures", NumberOfPictures);
                myCommand.Parameters.AddWithValue("@param_created_by", created_by);
                myCommand.ExecuteNonQuery();

            }
            else
            {

                query = "SELECT id FROM jos_content WHERE id = @param_oldArtId;";   // ПОЛУЧИМ РОДИТЕЛЬСКУЮ (старую)  СТАТЬИ 
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_oldArtId", old_Art_Id);
                znachenie = myCommand.ExecuteScalar().ToString();
                int id_Old = Convert.ToInt32(znachenie);

                query = "SELECT x FROM jos_content WHERE id = @param_id_Old;";   // x
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                znachenie = myCommand.ExecuteScalar().ToString();
                x = Convert.ToDouble(znachenie);

                query = "SELECT y FROM jos_content WHERE id = @param_id_Old;";   //  y
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                znachenie = myCommand.ExecuteScalar().ToString();
                y = Convert.ToDouble(znachenie);

                query = "SELECT Width FROM jos_content WHERE id = @param_id_Old;";   // Width, 
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                znachenie = myCommand.ExecuteScalar().ToString();
                Width = Convert.ToDouble(znachenie);

                query = "SELECT Height FROM .jos_content WHERE id = @param_id_Old;";   // Height
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                znachenie = myCommand.ExecuteScalar().ToString();
                Height = Convert.ToDouble(znachenie);


                query = "SELECT NumberOfPictures FROM jos_content WHERE id = @param_id_Old;";   //  получим NumberOfPictures
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                znachenie = myCommand.ExecuteScalar().ToString();
                NumberOfPictures = Convert.ToInt32(znachenie);

                //запись данных старой категории @param_NumberOfPictures+1
                query = "UPDATE jos_content SET NumberOfPictures=@param_NumberOfPictures+1 WHERE id = @param_id_Old;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_NumberOfPictures", NumberOfPictures);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                myCommand.ExecuteNonQuery();

                //запись данных новой категории parent_id
                query = "UPDATE jos_content SET parent_id=@param_id_Old WHERE id = @param_id;";
                myCommand = new SqlCommand(query, myConnection);
                myCommand.Parameters.AddWithValue("@param_id_Old", id_Old);
                myCommand.Parameters.AddWithValue("@param_id", id);
                myCommand.ExecuteNonQuery();

            }
        }
        // запросим имя файла картинки
        query = "SELECT images FROM jos_conten` WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        JObject o = JObject.Parse(znachenie);

        string image_intro = (string)o["image_intro"];
        image_intro = image_intro.Replace('/', '\\');  // win сервер
        if (image_intro == "") image_intro = "images\\n.png";

        // запросим название статьи
        query = "SELECT title FROM jos_content WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        string title = znachenie;

        //  номер родительской категории + "-" +  alias статьи 
        query = "SELECT alias FROM jos_content WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        string alias = Convert.ToString(id) + "-" + znachenie;

        //Article_id
        query = "SELECT id FROM jos_content WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        string Article_id = znachenie;

        //Article Height
        query = "SELECT height FROM jos_content WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        Article_Height = Convert.ToDouble(znachenie);

        //Article Width
        query = "SELECT width FROM jos_content WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        Article_Width = Convert.ToDouble(znachenie);

        //запись данных - сбросим new из 0 в 1
        query = "UPDATE jos_content SET new=1 WHERE new=0;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.ExecuteNonQuery();

        //query = "INSERT INTO `jos_categories` (`asset_id`, `parent_id`, `lft`, `rgt`, `level`, `path`, `extension`, `title`, `alias`, `note`, `description`, `published`, `checked_out`, `checked_out_time`, `access`, `params`, `metadesc`, `metakey`, `metadata`, `created_user_id`, `created_time`, `modified_user_id`, `modified_time`, `hits`, `language`, `x`, `y`, `width`, `height`, `NumberOfPictures`) VALUES ('0', '0', '0', '0', '0', '', '', '" + title + "', '', '', '', '0', '0', '0000-00-00 00:00:00', '0', '', '', '', '', '0', '0000-00-00 00:00:00', '0', '0000-00-00 00:00:00', '0', '', '111', '222', '333', '444', '0');";
        //query = "INSERT INTO `jos_categories` (`asset_id`, `parent_id`, `lft`, `rgt`, `level`, `path`, `extension`, `title`, `alias`, `note`, `description`, `published`, `checked_out`, `checked_out_time`, `access`, `params`, `metadesc`, `metakey`, `metadata`, `created_user_id`, `created_time`, `modified_user_id`, `modified_time`, `hits`, `language`, `x`, `y`, `width`, `height`, `NumberOfPictures`) VALUES ('0', '0', '0', '0', '0', '', '', '" + title + "', '', '', '', '0', '0', '0000-00-00 00:00:00', '0', '', '', '', '', '0', '0000-00-00 00:00:00', '0', '0000-00-00 00:00:00', '0', '', '0', '0', '0', '0', '0');";
        //myCommand = new SqlCommand(query, myConnection);

        doc = XDocument.Load(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));

        int maxId = 0;

        foreach (XElement el in doc.Root.Elements())
        {
            maxId = maxId + 1;

            //foreach (XElement element in el.Elements())
            //    if( element.Name == "new") new1 = element.Value;

        }

        double koefPicS = 1;
        double koefPicX = 0;
        double koefPicY = 0;
        if (NumberOfPictures > 5 && NumberOfPictures < 32) // второй ряд картинок
        {
            koefPicS = 4;
            koefPicX = 0.1;
            koefPicY = 6;
        }

        x = x - Width * (0.2 + koefPicX);
        y = y - Height * (0.1 - 0.15 * (NumberOfPictures - koefPicY)) / koefPicS;
        Width = Width * 0.1 / koefPicS;
        Height = Height * 0.1 / koefPicS;
        if (Article_Width < Article_Height) // картинка не должны вылазить за рамки 
        {
            if (Article_Height != 0 && Article_Height != 0)
            {
                double koef = Article_Width / Article_Height +0.06;
                Width = koef * Width;
                Height = koef * Height;
            }
        }
 
        XElement Image = new XElement("Image",
             new XElement("FileName", image_intro),
             new XElement("x", x),
             new XElement("y", y),
             new XElement("Width", Width),
             new XElement("Height", Height),    
             new XElement("ZOrder", maxId),
             new XElement("Tag", title),
             new XElement("Alias", alias),
             new XElement("Article_id", Article_id));
        doc.Root.Add(Image);

        //сохраняем наш документ Metadata
        doc.Save(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));

        // сохраняем x,y,Width,Height в категории статьи
        query = "UPDATE jos_content SET x=@param_x WHERE id = @param_id;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_x", x);
        myCommand.Parameters.AddWithValue("@param_id", id);
        myCommand.ExecuteNonQuery();

        query = "UPDATE jos_content SET y=@param_y WHERE id = @param_id;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_y", y);
        myCommand.Parameters.AddWithValue("@param_id", id);
        myCommand.ExecuteNonQuery();

        query = "UPDATE jos_content SET Width=@param_Width WHERE id = @param_id;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_Width", Width);
        myCommand.Parameters.AddWithValue("@param_id", id);
        myCommand.ExecuteNonQuery();

        query = "UPDATE jos_content SET Height=@param_Height WHERE id = @param_id;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_Height", Height);
        myCommand.Parameters.AddWithValue("@param_id", id);
        myCommand.ExecuteNonQuery();
 

    }
    /// <summary>
    ///- изменение аватарки
    /// </summary>
    private void UserAvatarChange(SqlConnection myConnection)// - создание аватарки
    {
        // user_id 
        //        query = "SELECT id FROM jos_users WHERE newUser=0;";
        //      myCommand = new SqlCommand(query, myConnection);
        //    znachenie = myCommand.ExecuteScalar().ToString();
        //  string user_id = znachenie;

        string znachenie;
        string query;
        XDocument doc;
        SqlCommand myCommand;

        // запросим  username
        query = "SELECT username FROM jos_users WHERE newUser < 0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        string username = znachenie;


        // запросим  avatar_url
        query = "SELECT avatar_url FROM jos_users WHERE  username=@param_username;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_username", username);
        znachenie = myCommand.ExecuteScalar().ToString();
        string newAvatar = znachenie;

        // запросим  avatar_url
        query = "SELECT newUser FROM jos_users WHERE  username=@param_username;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_username", username);
        znachenie = myCommand.ExecuteScalar().ToString();
        double newUser = Convert.ToDouble(znachenie);



        if (newAvatar != "")
        {
            //заменим avatar_url
            query = "UPDATE jos_users SET avatar_url=@param_avatar_url  WHERE  username=@param_username;";
            myCommand = new SqlCommand(query, myConnection);
            myCommand.Parameters.AddWithValue("@param_username", username);
            myCommand.Parameters.AddWithValue("@param_avatar_url", newAvatar);
            myCommand.ExecuteNonQuery();
            doc = XDocument.Load(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));
            //заменим avatar в Metadata.xml
            IEnumerable<XElement> tracks = doc.Root.Descendants("Image").Where(t => t.Element("Tag").Value == username).ToList();//Выберем ВСЕ теги с именем username
            foreach (XElement t in tracks) t.Element("FileName").Value = newAvatar;    //Заменим avatar ДЛЯ ВСЕХ пользоваателей с именем username
            doc.Save(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));
         }
        //заменим newUser на положительное
        newUser = -newUser;
        query = "UPDATE jos_users SET newUser=@param_newUser  WHERE  username=@param_username;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_username", username);
        myCommand.Parameters.AddWithValue("@param_newUser", newUser);
        myCommand.ExecuteNonQuery();
    }
    /// <summary>
    ///- создание аватарки
    /// </summary>
    private void NewUserAvatar(SqlConnection myConnection) //  - изменение аватарки
    {
        // user_id 
        //        query = "SELECT id FROM `my_site_db689`.`jos_users` WHERE `newUser`=0;";
        //      myCommand = new SqlCommand(query, myConnection);
        //    znachenie = myCommand.ExecuteScalar().ToString();
        //  string user_id = znachenie;
 
        string znachenie;
        string query;
        double Width;
        double Height;
        XDocument doc;
        SqlCommand myCommand;
        //Если запрос нам возвращает одно значение (надо быть уверенным что запрос вернёт именно одно значение иначе будет ошибка)./////
        // запросим  username
        query = "SELECT username FROM jos_users WHERE newUser=0;";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        string username = znachenie;


        // запросим номер max newUser
        query = "SELECT MAX(newUser) FROM jos_users";
        myCommand = new SqlCommand(query, myConnection);
        znachenie = myCommand.ExecuteScalar().ToString();
        int newUser = Convert.ToInt32(znachenie);

        doc = XDocument.Load(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));

        query = "SELECT x FROM jos_users WHERE newUser = @param_newUser";   // x
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_newUser", newUser);
        znachenie = myCommand.ExecuteScalar().ToString();
        double x = Convert.ToDouble(znachenie);

        query = "SELECT y FROM jos_users WHERE newUser = @param_newUser";   // y
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_newUser", newUser);
        znachenie = myCommand.ExecuteScalar().ToString();
        double y = Convert.ToDouble(znachenie);
        Width = 0.024; ///???
        Height = 0.27;

        //  простейшее фиксированное смещение для новой картинки
        y = y + 0.1;

        //запись данных категории @param_x
        query = "UPDATE jos_users SET x=@param_x  WHERE newUser=0;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_x", x);
        myCommand.ExecuteNonQuery();
        //запись данных категории @param_y
        query = "UPDATE jos_users SET y=@param_y  WHERE newUser=0;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_y", y);
        myCommand.ExecuteNonQuery();


        int maxId = 0;

        foreach (XElement el in doc.Root.Elements())
        {
            maxId = maxId + 1;

            //foreach (XElement element in el.Elements())
            //    if( element.Name == "new") new1 = element.Value;

        }

        string image_User = "images\\n.png";
        XElement Image = new XElement("Image",
             new XElement("FileName", image_User),
             new XElement("x", x),
             new XElement("y", y),
             new XElement("Width", Width),
             new XElement("Height", Height),
             new XElement("ZOrder", maxId),
             new XElement("Tag", username),
             new XElement("Alias", ""),
             new XElement("Article_id", ""));
        doc.Root.Add(Image);

        //сохраняем наш документ
        doc.Save(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));

        //запись данных категории @param_newUser+1
        query = "UPDATE jos_users SET newUser=@param_newUser+1  WHERE newUser=0;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.Parameters.AddWithValue("@param_newUser", newUser);
        myCommand.ExecuteNonQuery();


        //запись данных - сбросим newUser из 0 в 1
        query = "UPDATE jos_users SET newUser=1 WHERE newUser=0;";
        myCommand = new SqlCommand(query, myConnection);
        myCommand.ExecuteNonQuery();
    }


	/// <summary>
	/// Generate the deep zoom content using a CollectionCreator.
	/// </summary>
	private void CreateDeepZoom()
	{
 		CollectionCreator creator = new CollectionCreator();
		List<Image> images = new List<Image>();
		XDocument doc = XDocument.Load(HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/Metadata.xml"));
		var imageElements = doc.Root.Elements("Image");
		double aspectRatio = double.Parse(doc.Root.Element("AspectRatio").Value);
		//Populates a list of Microsoft.DeepZoomTools.Image objects using the value provided in Metadata.xml.
		foreach (XElement imageElement in imageElements)
		{
			int zOrder = int.Parse(imageElement.Element("ZOrder").Value);
			double width = 1d / double.Parse(imageElement.Element("Width").Value);
			images.Add(new Image(HttpContext.Current.Server.MapPath("~/SourceImages/" + imageElement.Element("FileName").Value))
			{
				ViewportWidth = width,
				ViewportOrigin = new Point(double.Parse(imageElement.Element("x").Value) * -width, double.Parse(imageElement.Element("y").Value) * -width / aspectRatio),
			});
		}
		creator.Create(images, HttpContext.Current.Server.MapPath("~/ClientBin/GeneratedImages/dzc_output"));
	}
}
