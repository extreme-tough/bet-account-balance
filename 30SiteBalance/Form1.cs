using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using ExcelNS = Microsoft.Office.Interop.Excel;


namespace _30SiteBalance
{
    public partial class Form1 : Form
    {

        int BrowseTimeout ;
        DateTime navStarted;
        public Boolean Autopilot = true;
        private string logFile;
        private string outputFile;
        string Current, Pending, Available, Casino, Risk;
        bool LastOneError=false;
        int CurrentLoop = 1, Tries;

        ExcelNS.Application oExcel;
        ExcelNS.Workbook oWB;
        ExcelNS.Worksheet oSheet;

        public Form1()
        {
            InitializeComponent();
            logFile = Application.StartupPath + @"\applog.txt";
            BrowseTimeout = int.Parse(ConfigurationManager.AppSettings["BrowseTimeout_Secs"]);
            Tries = int.Parse(ConfigurationManager.AppSettings["Retries"]);
            outputFile = ConfigurationManager.AppSettings["OutputPath"] + @"\Balance.xls";
        }

        private void Log(string Message)
        {
            //if (Autopilot)
                File.AppendAllText(logFile, DateTime.Now + " : " + Message + Environment.NewLine);
            //else
                //MessageBox.Show(Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LastOneError = true;
        }

        private Boolean isPageError()
        {
            try
            {
                if (webBrowser1.Document.Title.Contains("cannot display the webpage"))
                    return true;

                if (webBrowser1.Document.Title.Contains("Internal Server Error"))
                    return true;
                return false;
            }
            catch { return false; }
        }
        private void Process_Click(object sender, EventArgs e)
        {
            StartProcess();
        }
        public void StartProcess()
        {
            string[] siteDetails = File.ReadAllLines(Application.StartupPath + @"\sites.info");
            string siteURL, UserID, Password;
            string[] siteParts;
            int currentRow = 4;
            float CurBal;
            if (File.Exists(logFile))
                File.Delete(logFile);


            File.WriteAllText(logFile, "");
            try
            {
                File.Copy(Application.StartupPath + @"\template.xls", outputFile,true);
            }
            catch {
                MessageBox.Show(outputFile + " is already open. Close and try again", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            oExcel = new ExcelNS.Application();
            oExcel.DisplayAlerts = false;
            oWB = oExcel.Workbooks.Open(outputFile, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            oSheet = (ExcelNS.Worksheet)oWB.Sheets[1];
            ExcelNS.Style oStyle = null;
            ExcelNS.Range range;

            //oStyle.Font.Color = -16776961;

            webBrowser1.ScriptErrorsSuppressed = true;
            foreach (string site in siteDetails)
            {
                Current = "";
                Pending="";
                Available="";
                Casino="";
                Risk="";
                try
                {
                    siteParts = site.Split(new string[] { " " }, StringSplitOptions.None);
                    siteURL = siteParts[0];
                    siteParts = siteParts[1].Split(new string[] { "/" }, StringSplitOptions.None);
                    UserID = siteParts[0];
                    Password = siteParts[1];
                }
                catch(Exception ex)
                {
                    Log("Configuration file is not in proper format - " + ex.Message);
                    lblStatus.Text = "Stopped with error.";
                    lblStatus.Refresh();
                    return;
                }
                lblStatus.Text = "Processing " + siteURL + "...";
                lblStatus.Refresh();
                CurrentLoop = 1;
                LastOneError = false;
                while (CurrentLoop <= Tries)
                {
                    ProcessURL(siteURL, UserID, Password);
                    
                    if (!LastOneError)
                        break;
                    CurrentLoop++;
                    lblStatus.Text = "Retrying " + siteURL + "...(Try - " + CurrentLoop.ToString() + ")";
                    lblStatus.Refresh();
                    
                    lblStatus.Refresh();
                }
                range = oSheet.get_Range("A" + currentRow, Type.Missing);
                range.Cells.Value2 = siteURL;
                range = oSheet.get_Range("B" + currentRow, Type.Missing);

                Current = Current.Replace(",", "");
                Pending = Pending.Replace(",", "");
                Available = Available.Replace(",", "");
                Risk = Risk.Replace(",", "");
                Casino = Casino.Replace(",", "");

                if (Current != "")
                {
                    CurBal = float.Parse(Current);
                    range.Cells.Value2 = CurBal;

                    if (CurBal < 0)
                        range.Font.Color = -16776961;
                }

                if (siteURL.Contains("youwager.com"))
                {
                    if (Pending == "")
                        CurBal = 0 - 50000;
                    else
                        CurBal = float.Parse(Pending) - 50000;
                    range.Cells.Value2 = CurBal;

                    if (CurBal < 0)
                        range.Font.Color = -16776961;
                }

                if (Pending != "")
                {
                    range = oSheet.get_Range("C" + currentRow, Type.Missing);
                    range.Cells.Value2 = float.Parse( Pending);

                    if (float.Parse(Pending) < 0)
                        range.Font.Color = -16776961;
                }
                if (Available != "")
                {
                    range = oSheet.get_Range("D" + currentRow, Type.Missing);
                    range.Cells.Value2 = float.Parse(Available);

                    if (float.Parse(Available) < 0)
                        range.Font.Color = -16776961;
                }

                if (Risk != "")
                {
                    range = oSheet.get_Range("E" + currentRow, Type.Missing);
                    range.Cells.Value2 = float.Parse(Risk);

                    if (float.Parse(Risk) < 0)
                        range.Font.Color = -16776961;
                }

                if (Casino != "")
                {
                    range = oSheet.get_Range("F" + currentRow, Type.Missing);
                    range.Cells.Value2 = float.Parse(Casino);

                    if (float.Parse(Casino) < 0)
                        range.Font.Color = -16776961;
                }
                //File.AppendAllText(outputFile,siteURL + ","+ Current.Replace(",","") + "," + Pending.Replace(",","") + "," + Available.Replace(",","") + ","  + Risk.Replace(",","") + "," +  Casino.Replace(",","") + Environment.NewLine);
                currentRow++;
            }
            if (!Autopilot)
                MessageBox.Show("Scrapping is complete", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lblStatus.Text = "Done.";
            lblStatus.Refresh();
            oWB.Save();
            oWB.Saved = true;
            try
            {
                oWB.Close(false, Type.Missing, Type.Missing);
            }
            catch { }
            oExcel.Quit();
            oExcel = null;
        }

        private void ProcessURL(string URL, string ID, string pwd)
        {
            
            
            URL = URL.ToLower();

            if (URL.Contains("explorersports.net"))
                URL = "http://www.explorersports.net/Login.asp";
            if (URL.Contains("zodiacsportsbook.com"))
                URL = "http://www.zodiacsportsbook.com/Login.asp";
            if (URL.Contains("black32.com"))
                URL = "http://www.black32.com/Login.aspx";
            if (URL.Contains("getitinnow.com"))
                URL = "http://www.getitinnow.com/Login.aspx";
            if (URL.Contains("justwagers.com"))
                URL = "http://www.justwagers.com/index.asp";
            if (URL.Contains("betsharpsports"))
                URL = "http://ww2.betsharpsports.com/";

            navStarted = DateTime.Now;
            webBrowser1.Navigate("About:blank");            
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
                if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                {
                    webBrowser1.Stop();
                    break;
                }
            }

            navStarted = DateTime.Now;
            webBrowser1.Navigate(URL);            
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
                if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                {
                    webBrowser1.Stop();
                    break;
                }
            }

            if (URL.Contains("globalsides.com") )
            {
                globalsides( ID, pwd);                                
            }            
            if (URL.Contains("jazzsports.com"))
            {
                jazzsports(ID, pwd);
            }
            if (URL.Contains("playatmm.com"))
            {
                playatmm(ID, pwd);
            }
            if (URL.Contains("visionwager.com"))
            {
                visionwager(ID, pwd);
            }
            if (URL.Contains("actiononesports.com"))
            {
                actiononesports(ID, pwd);
            }
            if (URL.Contains("bettropics.com"))
            {
                bettropics(ID, pwd);
            }
            if (URL.Contains("cashbox98.com"))
            {
                cashbox98(ID, pwd);
            }
            if (URL.Contains("explorersports.net"))
            {
                explorersports(ID, pwd);
            }
            if (URL.Contains("ezplay2001.com"))
            {
                ezplay2001(ID, pwd);
            }
            if (URL.Contains("justwagers.com"))
            {
                justwagers(ID, pwd);
            }
            if (URL.Contains("zodiacsportsbook.com"))
            {
                zodiacsportsbook(ID, pwd);
            }
            if (URL.Contains("betevo.com"))
            {
                betevo(ID, pwd);
            }
            if (URL.Contains("betmsg.com"))
            {
                betmsg(ID, pwd);
            }
            if (URL.Contains("betsharpsports.com"))
            {
                betsharpsports(ID, pwd);
            }
            if (URL.Contains("bettordays.com"))
            {
                bettordays(ID, pwd);
            }
            if (URL.Contains("black32.com"))
            {
                black32(ID, pwd);
            }
            if (URL.Contains("linesforsale.com"))
            {
                linesforsale(ID, pwd);
            }
            if (URL.Contains("major65.com"))
            {
                major65(ID, pwd);
            }
            if (URL.Contains("youwager.com"))
            {
                youwager(ID, pwd);
            }
            if (URL.Contains("2betez.com"))
            {
                _2betez(ID, pwd);
            }
            if (URL.Contains("mywagerlive.com"))
            {
                mywagerlive(ID, pwd);
            }
            if (URL.Contains("readytowager.com"))
            {
                readytowager(ID, pwd);
            }
            if (URL.Contains("getitinnow.com"))
            {
                getitnow(ID, pwd);
            }
            if (URL.Contains("fastwagerlive.com"))
            {
                fastwagerlive(ID, pwd);
            }
            if (URL.Contains("bellaction.com"))
            {
                bellaction(ID, pwd);
            }
            if (URL.Contains("pinnaclesports.com"))
            {
                pinnaclesports(ID, pwd);
            }
            if (URL.Contains("betbuckeyesports.com"))
            {
                betbuckeyesports(ID, pwd);
            }
            if (URL.Contains(".betez.com"))
            {
                betez(ID, pwd);
            }
            if (URL.Contains("wager123.com"))
            {
                wager123(ID, pwd);
            }
            if (URL.Contains("betrr.com"))
            {
                betrr(ID, pwd);
            }
            if (URL.Contains("justbetnow.com"))
            {
                justbetnow(ID, pwd);
            }
            if (URL.Contains("citybet.com"))
            {
                citybet(ID, pwd);
            }
            if (URL.Contains(".2espn.com"))
            {
                _2espn(ID, pwd);
            }
        }

        private void justwagers(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Justwagers - site load error");
                        return;
                    }
                        

                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }


                navStarted = DateTime.Now;
                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on Justwagers loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on Justwagers reading : " + ex.Message);
            }
        }
        private void globalsides( string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Url.ToString().Contains("/sportsbook/"))
                            break;
                    }
                    catch { }

                    if (isPageError())
                    {
                        Log("GlobalSides - site load error");
                        return;
                    }

                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }

                navStarted = DateTime.Now;
                webBrowser1.Navigate("http://www.globalsides.com/sportsbook/includes/rightframe.cfm");
                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log("Error on globalsides loading : " + ex.Message);
            }
            try
            {
                HtmlElement oElem = webBrowser1.Document.GetElementsByTagName("TABLE")[0].GetElementsByTagName("TR")[0].GetElementsByTagName("TABLE")[1];
                Current = oElem.GetElementsByTagName("TR")[0].GetElementsByTagName("TD")[1].InnerText;
                Pending = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[1].InnerText;
                Available = oElem.GetElementsByTagName("TR")[2].GetElementsByTagName("TD")[1].InnerText;
                Casino = oElem.GetElementsByTagName("TR")[3].GetElementsByTagName("TD")[1].InnerText;
            }
            catch (Exception ex)
            {
                Log("Error on globalsides reading : " + ex.Message);
            }

        }
        private void jazzsports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("account").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Jazzsports - site load error");
                        return;
                    }

                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }


                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on jazzsports loading : " + ex.Message);
            }
            //Current = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblCurrentBalance").InnerText.Replace("USD", "");
            //Available = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblRealAvailBalance").InnerText.Replace("USD", "");
            //Risk = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblAmountAtRisk").InnerText.Replace("USD", "");
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_lbCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_lbAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_lbAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on jazzsports reading : " + ex.Message);
            }
        }
        private void playatmm(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("txtLoginClient").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("txtPasswordClient").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");                
            }
            catch (Exception ex)
            {
                Log("Error on playatmm loading : " + ex.Message);
            }

            navStarted = DateTime.Now;
            try
            {                
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance"))
                        {
                            break;
                        }
                    }
                    catch{}
                    if (isPageError())
                    {
                        Log("Playatmm - site load error");
                        return;
                    }
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on playatmm waiting : " + ex.Message);
            }
            try
            {
                HtmlElement oElem = webBrowser1.Document.Forms[0].GetElementsByTagName("TABLE")[0].GetElementsByTagName("TABLE")[1].GetElementsByTagName("TR")[1];
                Current = oElem.GetElementsByTagName("TD")[1].InnerText;
                Available = oElem.GetElementsByTagName("TD")[2].InnerText;
                Pending = oElem.GetElementsByTagName("TD")[3].InnerText;
            }
            catch (Exception ex)
            {
                Log("Error on playatmm reading : " + ex.Message);
            }
        }
        private void visionwager(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_LoginForm1__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_LoginForm1__Password").SetAttribute("value", pwd);
                //webBrowser1.Document.Forms[0].InvokeMember("submit");
                webBrowser1.Document.GetElementById("ctl00_LoginForm1_BtnSubmit").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Visionwager - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }


                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on visionwager loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on visionwager reading : " + ex.Message);
            }
        }
        private void actiononesports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("customerID").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Please read"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Actiononesports - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Log("Error on actiononesports loading : " + ex.Message);
            }
            try
            {
                navStarted = DateTime.Now;
                webBrowser1.Navigate("http://www.actiononesports.com/sys/NwSportSelection.asp");

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on actiononesports reloading : " + ex.Message);
            }
            try
            {
            Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on actiononesports reading : " + ex.Message);
            }
        }
        private void bettropics(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");
                string oldUrl = webBrowser1.Url.ToString();

                navStarted = DateTime.Now;
                while (webBrowser1.Url.ToString() == oldUrl)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Log("Error on bettropics loading : " + ex.Message);
            }
            try
            {
                navStarted = DateTime.Now;

                webBrowser1.Navigate("http://www.bettropics.com/wagermenu.asp");

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on bettropics reloading : " + ex.Message);
            }

            try
            {
                string sLine = webBrowser1.Document.Body.InnerText.Split(new string[] { System.Environment.NewLine }, System.StringSplitOptions.None)[2];
                string[] fields = sLine.Split(new string[] { ":" }, StringSplitOptions.None);
                Current = fields[2].Replace("Total Pending", "").Replace("USD", "").Trim();
                Pending = fields[3].Replace("Non-Posted Casino", "").Replace("USD", "").Trim();
                Casino = fields[4].Trim();
            }
            catch (Exception ex)
            {
                Log("Error on bettropics reading : " + ex.Message);
            }
        }
        private void cashbox98(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");
                string oldUrl = webBrowser1.Url.ToString();

                while (webBrowser1.Url.ToString() == oldUrl)
                {
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on cashbox98 loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on cashbox98 reading : " + ex.Message);
            }
        }
        private void explorersports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("ExporerSports - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on explorersports loading : " + ex.Message);
            }
            try
            {
                string[] fields = webBrowser1.Document.GetElementById("header").GetElementsByTagName("DIV")[2].InnerHtml.Split(new string[] { "<BR>" }, StringSplitOptions.None);
                Current = fields[1].Replace("Current Balance:", "");
                Available = fields[2].Replace("Available:", "");
                Pending = fields[3].Replace("Total Pending:", "").Replace("USD", "").Replace("&nbsp;", "");
                Casino = fields[4].Replace("Non-Posted Casino:", "").Replace("USD", "");
                LastOneError = false;
            }
            catch (Exception ex)
            {
                Log("Error on explorersports reading : " + ex.Message);
            }
        }
        private void ezplay2001(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Ezplay2001 - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on ezplay2001 loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on ezplay2001 reading : " + ex.Message);
            }
        }        
        private void zodiacsportsbook(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Zodiacsportsbook - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }            
            catch (Exception ex)
            {
                Log("Error on zodiacsportsbook loading : " + ex.Message);
            }

            try
            {
                string[] fields = webBrowser1.Document.GetElementById("header").GetElementsByTagName("DIV")[2].InnerHtml.Split(new string[] { "<BR>" }, StringSplitOptions.None);
                Current = fields[1].Replace("Current Balance:", "");
                Available = fields[2].Replace("Available:", "");
                Pending = fields[3].Replace("Total Pending:", "").Replace("USD", "").Replace("&nbsp;", "");
                Casino = fields[4].Replace("Non-Posted Casino:", "").Replace("USD", "");
            }            
            catch (Exception ex)
            {
                Log("Error on zodiacsportsbook reading : " + ex.Message);
            }
        }
        private void betevo(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("customerID").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("betevo - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betevo loading : " + ex.Message);
            }

            try
            {
                HtmlElement oElem = webBrowser1.Document.Body.GetElementsByTagName("TABLE")[3];
                Current = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[1].InnerText.Replace("$", "") ;
                Risk = oElem.GetElementsByTagName("TR")[2].GetElementsByTagName("TD")[1].InnerText.Replace("$", "");
                Casino = oElem.GetElementsByTagName("TR")[3].GetElementsByTagName("TD")[1].InnerText.Replace("$", "");
            }
            catch (Exception ex)
            {
                Log("Error on betevo reading : " + ex.Message);
            }
        }
        private void betmsg(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("customerID").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");
                string oldUrl = webBrowser1.Url.ToString();

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Betmsg - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betmsg loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on betmsg reading : " + ex.Message);
            }
        }
        private void betsharpsports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__Password").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Betsharpsports - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betsharpsports loading : " + ex.Message);
            }
        }
        private void bettordays(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Bettordays - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on bettordays loading : " + ex.Message);
            }

            try
            {
                HtmlElement oElem = webBrowser1.Document.GetElementsByTagName("TABLE")[0].GetElementsByTagName("TABLE")[0];
                Current = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[1].InnerText.Replace("USD", "").Trim();
                Available = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[2].InnerText.Replace("USD", "").Trim();
                Pending = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[3].InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on bettordays reading : " + ex.Message);
            }
        }
        private void black32(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin_BtnSubmit").InvokeMember("click");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Black32 - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on black32 loading : " + ex.Message);
            }
            
        }
        private void linesforsale(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");                

                //navStarted = DateTime.Now;
                //while (true)
                //{
                //    if (DateTime.Now >= navStarted.AddSeconds(15))
                //    {
                //        webBrowser1.Stop();
                //        break;
                //    }
                //    Application.DoEvents();
                //}
            }
            catch (Exception ex)
            {
                Log("Error on linesforsale loading : " + ex.Message);
            }

            try
            {
                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Lineforsale - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Log("Error on linesforsale reloading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on linesforsale reading : " + ex.Message);
            }
        }
        private void major65(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_ctl01__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_ctl01__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_ctl01_BtnSubmit").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Major65 - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }


                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on major65 loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_ctl01_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_ctl01_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_ctl01_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on major65 reading : " + ex.Message);
            }
        }
        private void youwager(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("customerid").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Youwager - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on youwager loading : " + ex.Message);
            }
            try
            {
                foreach (HtmlElement oElem in webBrowser1.Document.GetElementsByTagName("DIV"))
                {
                    if (oElem.OuterHtml.Contains("<DIV class=section><B>Balance:"))
                    {
                        Pending = oElem.GetElementsByTagName("A")[0].InnerText.Replace("USD", "");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on youwager reading : " + ex.Message);
            }
        }
        private void _2espn(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Url.ToString().Contains("/sportsbook/"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("2Espn - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;


                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on 2espn loading : " + ex.Message);
            }


            navStarted = DateTime.Now;
            webBrowser1.Navigate("http://www.2espn.com/sportsbook/includes/rightframe.cfm");
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
                if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                {
                    webBrowser1.Stop();
                    break;
                }
            }


            try
            {
                HtmlElement oElem = webBrowser1.Document.Body.GetElementsByTagName("TABLE")[2];
                Current = oElem.GetElementsByTagName("TR")[0].GetElementsByTagName("TD")[1].InnerText;
                Pending = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[1].InnerText;
                Available = oElem.GetElementsByTagName("TR")[2].GetElementsByTagName("TD")[1].InnerText;
            }
            catch (Exception ex)
            {
                Log("Error on 2espn reading : " + ex.Message);
            }
        }
        private void _2betez(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                        if (isPageError())
                        {
                            Log("2betez - site load error");
                            return;
                        }
                    }
                    catch { }

                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;


                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on _2betez loading : " + ex.Message);
            }


            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_AccountFigures3_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on _2betez reading : " + ex.Message);
            }
        }
        private void mywagerlive(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00$ctlLogin$_Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00$ctlLogin$BtnSubmit").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Mywagerlive - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on mywagerlive loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblCurrentBalance").InnerText.Replace("USD", "");
                Available = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblRealAvailBalance").InnerText.Replace("USD", "");
                Risk = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblAmountAtRisk").InnerText.Replace("USD", "");
            }
            catch (Exception ex)
            {
                Log("Error on mywagerlive reading : " + ex.Message);
            }
        }
        private void readytowager(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("customerID").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Readytowager - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on readytowager loading : " + ex.Message);
            }
            try
            {
                string Data = webBrowser1.Document.Body.InnerHtml;
                int nPos1 = Data.IndexOf("popup(");
                int nPos2 = Data.IndexOf(")", nPos1);
                Data = Data.Substring(nPos1 + 6, nPos2 - nPos1 - 6);
                string[] fields = Data.Split(new string[] { "<td align=right>" }, StringSplitOptions.None);
                Current = fields[1].Split(new string[] { "</td>" }, StringSplitOptions.None)[0];
                Pending = fields[3].Split(new string[] { "</td>" }, StringSplitOptions.None)[0];
                Available = fields[4].Split(new string[] { "</td>" }, StringSplitOptions.None)[0];
            }
            catch (Exception ex)
            {
                Log("Error on readytowager reading : " + ex.Message);
            }

        }
        private void getitnow(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_MainContent_ctlLogin_BtnSubmit").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Getitonnow - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on getitnow loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_AccountFigures2_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_AccountFigures2_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_AccountFigures2_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on getitnow reading : " + ex.Message);
            }
        }
        private void fastwagerlive(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_ctlLogin__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_ctlLogin_BtnSubmit").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Fastwagerlive - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on fastwagerlive loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on fastwagerlive reading : " + ex.Message);
            }
        }
        private void bellaction(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].InvokeMember("submit");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Current Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Bellaction - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on bellaction loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_WagerContent_AccountFigures1_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on bellaction reading : " + ex.Message);
            }
        }
        private void pinnaclesports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_LF_LB").InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Balance:"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Pinnaclesports - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on pinnaclesports loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("balance").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on pinnaclesports reading : " + ex.Message);
            }
        }
        private void betbuckeyesports(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("Forms Edit Field1").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("Forms Edit Field2").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("Forms Button1").InvokeMember("click");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Betbuckeyesports - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betbuckeyesports loading : " + ex.Message);
            }

            try
            {
                HtmlElement oElem = webBrowser1.Document.GetElementById("table3");
                Current = oElem.GetElementsByTagName("TR")[1].GetElementsByTagName("TD")[1].InnerText.Replace("$", "");
                Risk = oElem.GetElementsByTagName("TR")[2].GetElementsByTagName("TD")[1].InnerText.Replace("$", "");
                Available = oElem.GetElementsByTagName("TR")[3].GetElementsByTagName("TD")[1].InnerText.Replace("$", "");
            }
            catch (Exception ex)
            {
                Log("Error on betbuckeyesports reading : " + ex.Message);
            }
        }
        private void betez(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("account").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("password").SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Betez - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }


                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betez loading : " + ex.Message);
            }
            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_controlTopLogo_TopFigures_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on betez reading : " + ex.Message);
            }
        }
        private void wager123(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.GetElementById("ctl00_LoginForm1__UserName").SetAttribute("value", ID);
                webBrowser1.Document.GetElementById("ctl00_LoginForm1__Password").SetAttribute("value", pwd);
                webBrowser1.Document.GetElementById("ctl00_LoginForm1_BtnSubmit").InvokeMember("click");



                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Wager123 - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on wager123 loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblCurrentBalance").InnerText.Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblRealAvailBalance").InnerText.Replace("USD", "").Trim();
                Risk = webBrowser1.Document.GetElementById("ctl00_crlAccFiguresTop_lblAmountAtRisk").InnerText.Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on wager123 reading : " + ex.Message);
            }
        }
        private void betrr(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");

                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Betrr - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on betrr loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on betrr reading : " + ex.Message);
            }
        }
        private void justbetnow(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Justbetnow - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on justbetnow loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on justbetnow reading : " + ex.Message);
            }

        }
        private void citybet(string ID, string pwd)
        {
            try
            {
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[0].SetAttribute("value", ID);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[1].SetAttribute("value", pwd);
                webBrowser1.Document.Forms[0].GetElementsByTagName("input")[2].InvokeMember("click");


                navStarted = DateTime.Now;
                while (true)
                {
                    try
                    {
                        if (webBrowser1.Document.Body.InnerText.Contains("Available Balance"))
                            break;
                    }
                    catch { }
                    if (isPageError())
                    {
                        Log("Citybet - site load error");
                        return;
                    }
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                    Application.DoEvents();
                }

                navStarted = DateTime.Now;

                while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (DateTime.Now >= navStarted.AddSeconds(BrowseTimeout))
                    {
                        webBrowser1.Stop();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error on citybet loading : " + ex.Message);
            }

            try
            {
                Current = webBrowser1.Document.GetElementById("currentBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Pending = webBrowser1.Document.GetElementById("pendingWagerBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Available = webBrowser1.Document.GetElementById("availableBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
                Casino = webBrowser1.Document.GetElementById("casinoBalance").InnerText.Replace("$", "").Replace("USD", "").Trim();
            }
            catch (Exception ex)
            {
                Log("Error on citybet reading : " + ex.Message);
            }
        }



        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
    }
}
