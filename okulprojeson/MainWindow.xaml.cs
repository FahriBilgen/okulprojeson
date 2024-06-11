using System;
using System.Data.SQLite; // SQLite için gerekli
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Windows.Media.Animation; // Animasyonlar için gerekli
using System.Windows.Threading; // DispatcherTimer için gerekli
using System.Text; // StringBuilder için gerekli

namespace StoryGenerator
{
    public partial class MainWindow : Window
    {
        private HttpClient chatGptClient;
        private HttpClient dallEClient;
        private const int maxRetries = 5;
        private const int retryDelay = 2000; // 2 saniye
        private string dbFile = "UserData.db"; // Veritabanı dosyası
        private DispatcherTimer promptTimer;
        private DispatcherTimer storyTimer;
        private string fullPromptText = "Bir zamanlar uzak diyarlarda...";
        private string fullStoryText;
        private int currentPromptIndex;
        private int currentStoryIndex;
        private StringBuilder currentPromptText;
        private StringBuilder currentStoryText;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase(); // Veritabanını başlat
            ShowLoginScreen(); // Giriş ekranını göster
        }

        private void InitializeDatabase()
        {
            if (!System.IO.File.Exists(dbFile))
            {
                SQLiteConnection.CreateFile(dbFile);
            }

            using (var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS UserLogins (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL,
                        Password TEXT NOT NULL,
                        LoginDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ShowStartupScreen()
        {
            StartupGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowLoginScreen()
        {
            StartupGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
        }

        private void ShowMainScreen()
        {
            StartupGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Collapsed;
            ShowExample();
        }

        private void ShowExample()
        {
            PromptTextBox.Text = string.Empty;
            StoryTextBlock.Text = string.Empty;
            StoryImage.Source = null;

            currentPromptIndex = 0;
            currentStoryIndex = 0;
            currentPromptText = new StringBuilder();
            currentStoryText = new StringBuilder();

            fullStoryText = "Bir zamanlar uzak diyarlarda yaşayan bir kral ve kraliçe vardı. Onların mutlu bir yaşamı vardı ve halkları tarafından çok sevilirdi. " +
                            "Bu kral ve kraliçenin üç çocuğu vardı: en büyükleri cesur bir şövalye, ortancaları zeki bir bilim insanı ve en küçüğü ise sanatçı ruhlu bir prenses. " +
                            "Her sabah saray bahçesinde kuş sesleri eşliğinde kahvaltı ederlerdi ve öğleden sonraları ise halkla birlikte çeşitli etkinliklere katılırlardı. " +
                            "Bir gün, krallığın yakınlarındaki ormanda garip olaylar meydana gelmeye başladı. Ağaçlar gizemli bir şekilde ışıldıyor ve geceleri ormandan tuhaf sesler geliyordu. " +
                            "Kral, en büyük oğlunu bu gizemi çözmesi için ormana gönderdi. Şövalye, ormanın derinliklerinde yaşayan eski bir büyücünün varlığını keşfetti. " +
                            "Bu büyücü, krallığı koruyan eski bir dosttu ve krallığa yaklaşan büyük bir tehlikeye karşı onları uyarmak için geri dönmüştü. " +
                            "Şövalye büyücüyü saraya getirdi ve büyücü, kral ve kraliçeye yaklaşan tehlikeyi anlattı. Bir kötü büyücü, krallığı ele geçirmek için karanlık güçlerle anlaşma yapmıştı. " +
                            "Kral ve kraliçe, halklarını korumak için bir araya geldi ve krallığın tüm kahramanlarını topladı. Cesur şövalyeler, zeki bilim insanları ve sanatçı ruhlu prenses, " +
                            "büyücüyle birlikte çalışarak kötü büyücüyü alt etmek için bir plan yaptılar. Günler süren mücadele sonunda, iyilik kazandı ve krallık tekrar barışa kavuştu. " +
                            "Kral ve kraliçe, halklarına olan sevgileri ve bağlılıkları sayesinde krallıklarını korumayı başardılar ve herkes yeniden mutlu bir şekilde yaşamaya devam etti.";

            promptTimer = new DispatcherTimer();
            promptTimer.Interval = TimeSpan.FromMilliseconds(10); // Prompt yazma hızı
            promptTimer.Tick += PromptTimer_Tick;
            promptTimer.Start();
        }

        private void PromptTimer_Tick(object sender, EventArgs e)
        {
            if (currentPromptIndex < fullPromptText.Length)
            {
                currentPromptText.Append(fullPromptText[currentPromptIndex]);
                PromptTextBox.Text = currentPromptText.ToString();
                currentPromptIndex++;
            }
            else
            {
                promptTimer.Stop();
                promptTimer = null;

                // Enter tuşu basma efekti
                PromptTextBox.Text += "\n";
                StoryTextBlock.Opacity = 1;

                storyTimer = new DispatcherTimer();
                storyTimer.Interval = TimeSpan.FromMilliseconds(10); // Masal yazma hızı
                storyTimer.Tick += StoryTimer_Tick;
                storyTimer.Start();
            }
        }

        private void StoryTimer_Tick(object sender, EventArgs e)
        {
            if (currentStoryIndex < fullStoryText.Length)
            {
                currentStoryText.Append(fullStoryText[currentStoryIndex]);
                StoryTextBlock.Text = currentStoryText.ToString();
                currentStoryIndex++;
            }
            else
            {
                storyTimer.Stop();
                storyTimer = null;

                // Görsel animasyonunu başlat
                StoryImage.Source = new BitmapImage(new Uri("https://masalist.com/images/Masallarin-Sihirli-Yolculugu-780x470.jpg.webp"));
                Storyboard imageStoryboard = (Storyboard)FindResource("ImageAnimation");
                imageStoryboard.Begin();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string chatGptApiKey = ChatGptApiKeyTextBox.Text;
            string dallEApiKey = DallEApiKeyTextBox.Text;

            if (string.IsNullOrWhiteSpace(chatGptApiKey) || string.IsNullOrWhiteSpace(dallEApiKey))
            {
                MessageBox.Show("Lütfen her iki API anahtarını da girin.");
                return;
            }

            chatGptClient = new HttpClient();
            chatGptClient.DefaultRequestHeaders.Clear();
            chatGptClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {chatGptApiKey}");

            dallEClient = new HttpClient();
            dallEClient.DefaultRequestHeaders.Clear();
            dallEClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {dallEApiKey}");

            ShowMainScreen();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Lütfen email ve şifre girin.");
                return;
            }

            SaveLogin(email, password); // Giriş bilgilerini kaydet

            ShowStartupScreen(); // Başlangıç ekranını göster
        }

        private void SaveLogin(string email, string password)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                connection.Open();
                string insertQuery = "INSERT INTO UserLogins (Email, Password) VALUES (@Email, @Password)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.ExecuteNonQuery();
                }
            }
        }

        private async void CreateStory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string prompt = PromptTextBox.Text; // Kullanıcıdan alınan prompt
                if (chatGptClient == null || dallEClient == null)
                {
                    MessageBox.Show("API anahtarları doğru ayarlanmadı.");
                    return;
                }

                var story = await GetCompletionAsync(prompt);
                fullStoryText = story;
                StoryTextBlock.Text = string.Empty; // Eski masal metnini temizle

                // Masal yazma animasyonunu başlat
                currentStoryIndex = 0;
                currentStoryText = new StringBuilder();
                storyTimer = new DispatcherTimer();
                storyTimer.Interval = TimeSpan.FromMilliseconds(10); // Masal yazma hızı
                storyTimer.Tick += StoryTimer_Tick;
                storyTimer.Start();

                // Görsel animasyonunu başlat (örneğin, dallEClient ile oluşturulan görsel URL'sini kullanabilirsiniz)
                string imageUrl = await GenerateImageAsync(prompt);
                StoryImage.Source = new BitmapImage(new Uri(imageUrl));
                Storyboard imageStoryboard = (Storyboard)FindResource("ImageAnimation");
                imageStoryboard.Begin();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Masal oluştururken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            if (chatGptClient == null)
                throw new InvalidOperationException("ChatGPT client is not initialized");

            var responseString = await MakeApiRequest(async () =>
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a storyteller." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 150
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                return await chatGptClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            });

            if (string.IsNullOrEmpty(responseString))
            {
                throw new Exception("API yanıtı boş veya null");
            }

            dynamic result = JsonConvert.DeserializeObject(responseString);
            if (result?.choices == null || result.choices.Count == 0)
            {
                throw new Exception("API yanıtı seçim içermiyor");
            }

            return result.choices[0]?.message?.content ?? "İçerik mevcut değil";
        }

        public async Task<string> GenerateImageAsync(string prompt)
        {
            if (dallEClient == null)
                throw new InvalidOperationException("DALL-E client is not initialized");

            var responseString = await MakeApiRequest(async () =>
            {
                var requestBody = new
                {
                    prompt = prompt,
                    n = 1,
                    size = "780x470"
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                return await dallEClient.PostAsync("https://api.openai.com/v1/images/generations", content);
            });

            if (string.IsNullOrEmpty(responseString))
            {
                throw new Exception("API yanıtı boş veya null");
            }

            dynamic result = JsonConvert.DeserializeObject(responseString);
            if (result?.data == null || result.data.Count == 0)
            {
                throw new Exception("API yanıtı veri içermiyor");
            }

            return result.data[0]?.url ?? "URL mevcut değil";
        }

        private async Task<string> MakeApiRequest(Func<Task<HttpResponseMessage>> apiRequest)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await apiRequest();
                    var statusCode = (int)response.StatusCode;
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Durum kodunu ve yanıt içeriğini loglama
                    Console.WriteLine($"Response Status Code: {statusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");

                    response.EnsureSuccessStatusCode(); // Başarısız bir HTTP isteğinde özel durum fırlatır.
                    return responseContent;
                }
                catch (HttpRequestException httpRequestException)
                {
                    var statusCode = httpRequestException.StatusCode.HasValue ? (int)httpRequestException.StatusCode.Value : 0;
                    Console.WriteLine($"HTTP Request Exception: {httpRequestException.Message}, Status Code: {statusCode}");

                    if (statusCode == 429)
                    {
                        // Too Many Requests hatası durumunda bekle ve yeniden dene
                        await Task.Delay(retryDelay);
                    }
                    else if (statusCode == 400)
                    {
                        // Bad Request hatası için özel işlem
                        MessageBox.Show($"Bad Request error: {httpRequestException.Message}");
                        break;
                    }
                    else
                    {
                        // Diğer HTTP hataları için hata mesajı göster
                        MessageBox.Show($"Request error: {httpRequestException.Message}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Diğer hatalar için hata mesajı göster
                    MessageBox.Show($"An error occurred: {ex.Message}");
                    break;
                }
            }

            return string.Empty;
        }
    }
}
