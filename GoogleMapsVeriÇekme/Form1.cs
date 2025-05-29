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

                string excelPath = SaveToExcel(businesses);

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
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                driver.Navigate().GoToUrl("https://www.google.com/maps?hl=en");
                WaitForPageFullLoad(driver);
                var searchBox = driver.FindElement(By.Id("searchboxinput"));
                searchBox.SendKeys(keywords);
                searchBox.SendKeys(OpenQA.Selenium.Keys.Enter);

                Thread.Sleep(7000); // geç yüklenme durumunda arttırılabilir!


                
                IWebElement scrollArea = driver.FindElement(By.XPath("//div[@role='feed']"));
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;

                bool moreResultsAvailable = true;

                while (moreResultsAvailable)
                {
                    int previousHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollArea));
                    jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", scrollArea);
                    Thread.Sleep(5000);

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
                            string address = "Adres Bulunamadı";
                            string website = "Website bulunamadı.";
                            string phone = "Telefon bilgisi bulunamadı.";
                            string rating = "Puanlama bulunamadı";
                            string review = "Değerlendirme bulunamadı";

                            result.Click();
                            Thread.Sleep(3000);

                            // İsim bilgisi
                            try
                            {
                                name = driver.FindElement(By.XPath("(//div[@role='main' and @aria-label]//h1)[last()]")).Text;                                
                            }
                            catch (NoSuchElementException) { }

                            //Açık Adres Biligisi
                            try
                            {
                                address = driver.FindElement(By.XPath("//button[contains(@aria-label,'Address:')]//div[@class='AeaXub']")).Text;
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


                            // yıldız bilgisi
                            try
                            {
                                rating = driver.FindElement(By.XPath("//span[contains(@aria-label, 'star')][preceding-sibling::span[1]]")).GetAttribute("aria-label");
                            }
                            catch (NoSuchElementException) { }


                            // değerlendirme bilgisi
                            try
                            {
                                review = driver.FindElement(By.XPath("//span[contains(@aria-label, 'review') and starts-with(text(), '(')]")).Text;
                            }
                            catch (NoSuchElementException) { }

                            businessList.Add(new BusinessInfo
                            {
                                Name = name,
                                Address = address,
                                Link = url,
                                Website = website,
                                Phone = phone,
                                Rating = rating,
                                Review = review,
                            });

                            
                            collectedBusinessUrls.Add(url);
                        }
                        catch
                        {
                             
                        }
                    }
                }
                return businessList;
            }
        }

        private void WaitForPageFullLoad(IWebDriver driver)
        {
            var js = (IJavaScriptExecutor)driver;
            new WebDriverWait(driver, TimeSpan.FromSeconds(30))
                .Until(d => js.ExecuteScript("return document.readyState").Equals("complete"));
        }

        private string SaveToExcel(List<BusinessInfo> businesses)
        {
            string fileName = $"Businesses_{DateTime.Now:yyyy-MM-dd}.xlsx";
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Businesses");

                var headers = new string[] { "İsim", "Açık Adres", "Puanlama", "Değerlendirme", "Telefon", "Web Sitesi", "Link" };
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cell(1, col + 1);
                    cell.Value = headers[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                for (int i = 0; i < businesses.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = businesses[i].Name;
                    worksheet.Cell(row, 2).Value = businesses[i].Address;
                    worksheet.Cell(row, 3).Value = businesses[i].Rating;
                    worksheet.Cell(row, 4).Value = businesses[i].Review;
                    worksheet.Cell(row, 5).Value = businesses[i].Phone;
                    worksheet.Cell(row, 6).Value = businesses[i].Website;
                    worksheet.Cell(row, 7).Value = businesses[i].Link;

                    if (i % 2 == 1)
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightCyan;
                    }
                }

                worksheet.Columns().AdjustToContents();
                worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                workbook.SaveAs(filePath);
            }
            return filePath;
        }

        private class BusinessInfo
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public string Link { get; set; }
            public string Website { get; set; }
            public string Phone { get; set; }
            public string Rating { get; set; }
            public string Review { get; set; }
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
