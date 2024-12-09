using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ClosedXML.Excel;
using System.IO;
using WebDriverManager.DriverConfigs.Impl;
using OpenQA.Selenium.Support.UI;
using System.Threading;
using WebDriverManager;

namespace GoogleMapsVeriÇekme
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string keywords = txtKeywords.Text.Trim();

            if (string.IsNullOrEmpty(keywords))
            {
                MessageBox.Show("Lütfen aramak istediğiniz anahtar kelimeleri girin.");
                return;
            }

            try
            {
                List<BusinessInfo> businesses = ScrapeGoogleMaps(keywords);

                // Verileri Excele kaydetme
                string excelPath = SaveToExcel(businesses);

                // Exceli başlatma
                System.Diagnostics.Process.Start("explorer.exe", excelPath);

                MessageBox.Show("Veriler başarıyla kaydedildi ve Excel dosyası açıldı!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}");
            }
        }

        private List<BusinessInfo> ScrapeGoogleMaps(string keywords)
        {
            List<BusinessInfo> businessList = new List<BusinessInfo>();
            HashSet<string> collectedBusinessUrls = new HashSet<string>();

            new DriverManager().SetUpDriver(new ChromeConfig());

            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--start-maximized");
            options.AddArgument("--lang=en");
            options.AddArgument("user-agent=<your-agent-string>");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                driver.Navigate().GoToUrl("https://www.google.com/maps?hl=en"); 
                var searchBox = driver.FindElement(By.Id("searchboxinput"));
                searchBox.SendKeys(keywords);
                searchBox.SendKeys(OpenQA.Selenium.Keys.Enter);

                Thread.Sleep(5000); // Sayfa yüklenmeden işleme başlıyorsa burdaki süreyi arttır!!!

                IWebElement scrollArea = driver.FindElement(By.XPath("//div[@role='feed']"));
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;

                bool moreResultsAvailable = true;

                while (moreResultsAvailable)
                {
                    int previousHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollArea));
                    jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", scrollArea);
                    Thread.Sleep(3000);

                    int newHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollArea));
                    if (newHeight == previousHeight)
                    {
                        moreResultsAvailable = false; 
                    }

                    var results = driver.FindElements(By.CssSelector(".Nv2PK"));
                    foreach (var result in results)
                    {
                        try
                        {

                            string url = result.FindElement(By.CssSelector("a")).GetAttribute("href");
                            if (collectedBusinessUrls.Contains(url))
                            {
                                continue; 
                            }

                            
                            string name = "İsim bulunamadı.";
                            string website = "Website bulunamadı.";
                            string phone = "Telefon bilgisi bulunamadı.";

                            result.Click();
                            Thread.Sleep(3000);

                            // İsim bilgisi
                            try
                            {
                                name = driver.FindElement(By.XPath("(//div[@role='main' and @aria-label]//h1)[last()]")).Text;
                            }
                            catch (NoSuchElementException) { }

                            // Web site bilgisi ve telefon bilgisi
                            try
                            {
                                website = driver.FindElement(By.XPath("//a[contains(@aria-label, 'Website')]")).GetAttribute("href");
                            }
                            catch (NoSuchElementException) { }

                            try
                            {
                                phone = driver.FindElement(By.XPath("//button[starts-with(@aria-label, 'Phone:')]")).Text;
                            }
                            catch (NoSuchElementException) { }

                            businessList.Add(new BusinessInfo
                            {
                                Name = name,
                                Link = url,
                                Website = website,
                                Phone = phone
                            });

                            
                            collectedBusinessUrls.Add(url);
                        }
                        catch
                        {
                            continue; 
                        }
                    }
                }
                return businessList;
            }
        }



        private string SaveToExcel(List<BusinessInfo> businesses)
        {
            string fileName = $"Businesses_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Businesses");
                worksheet.Cell(1, 1).Value = "İsim";
                worksheet.Cell(1, 2).Value = "Link";
                worksheet.Cell(1, 3).Value = "Web Sitesi";
                worksheet.Cell(1, 4).Value = "Telefon";

                for (int i = 0; i < businesses.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = businesses[i].Name;
                    worksheet.Cell(i + 2, 2).Value = businesses[i].Link;
                    worksheet.Cell(i + 2, 3).Value = businesses[i].Website;
                    worksheet.Cell(i + 2, 4).Value = businesses[i].Phone;
                }
                workbook.SaveAs(filePath);
            }
            return filePath;
        }

        private class BusinessInfo
        {
            public string Name { get; set; }
            public string Link { get; set; }
            public string Website { get; set; }
            public string Phone { get; set; }
        }

        bool isWhite = true;
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Dark Mode Özellikleri
            if(isWhite)
            {
                this.BackColor = Color.White;
                label1.ForeColor = Color.Black;
            }
            else
            {
                this.BackColor = Color.FromArgb(54, 54, 54);
                label1.ForeColor = Color.White;
            } 
            isWhite = !isWhite;
            
        }
    }
}
