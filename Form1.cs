using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace Praktika
{
    public partial class Form1 : Form
    {
        private TextBox txtSearch;
        private Button btnSearch;
        private ListBox listBooks;
        private TextBox txtDetails;
        private HttpClient client;

        public Form1()
        {
            InitializeComponent();

            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            this.Text = "📚 Поиск книг (Open Library)";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            CreateControls();
        }

        private void CreateControls()
        {
            txtSearch = new TextBox()
            {
                Location = new Point(20, 20),
                Width = 350,
                Font = new Font("Segoe UI", 11F)
            };

            btnSearch = new Button()
            {
                Text = "🔍 Найти книги",
                Location = new Point(380, 18),
                Width = 130,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.LightBlue
            };
            btnSearch.Click += BtnSearch_Click;

            listBooks = new ListBox()
            {
                Location = new Point(20, 60),
                Width = 250,
                Height = 400,
                Font = new Font("Segoe UI", 10F)
            };
            listBooks.SelectedIndexChanged += ListBooks_SelectedIndexChanged;

            txtDetails = new TextBox()
            {
                Location = new Point(290, 60),
                Width = 370,
                Height = 400,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Text = "Выберите книгу из списка, чтобы увидеть детали..."
            };

            this.Controls.Add(txtSearch);
            this.Controls.Add(btnSearch);
            this.Controls.Add(listBooks);
            this.Controls.Add(txtDetails);
        }

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            listBooks.Items.Clear();
            txtDetails.Text = "Поиск...";

            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                MessageBox.Show("Введите название книги или автора!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDetails.Text = "Выберите книгу из списка, чтобы увидеть детали...";
                return;
            }

            try
            {
                string query = txtSearch.Text.Trim().Replace(" ", "+");
                string url = "https://openlibrary.org/search.json?q=" + query;

                var response = await client.GetStringAsync(url);

                // Показываем, что пришло от сервера (для отладки)
                // MessageBox.Show(response.Substring(0, Math.Min(500, response.Length)));

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // ВАЖНО! Игнорируем регистр
                };
                var data = JsonSerializer.Deserialize<OpenLibraryResponse>(response, options);

                if (data != null && data.Docs != null && data.Docs.Count > 0)
                {
                    listBooks.Items.Clear();
                    foreach (var book in data.Docs)
                    {
                        string title = book.Title ?? "Без названия";
                        string author = (book.AuthorName != null && book.AuthorName.Length > 0)
                            ? book.AuthorName[0]
                            : "Автор неизвестен";
                        string year = book.FirstPublishYear?.ToString() ?? "Год неизвестен";

                        listBooks.Items.Add($"{title} — {author} ({year})");
                    }
                    listBooks.Tag = data.Docs;
                    txtDetails.Text = $"✅ Найдено книг: {data.Docs.Count}\n\nВыберите книгу из списка для просмотра деталей.";
                }
                else
                {
                    MessageBox.Show("По вашему запросу ничего не найдено.\nПопробуйте, например: 'Harry Potter'",
                        "Результаты поиска", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtDetails.Text = "Выберите книгу из списка, чтобы увидеть детали...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}\n\nПроверьте интернет-соединение.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtDetails.Text = "Произошла ошибка при загрузке данных.";
            }
        }

        private void ListBooks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBooks.SelectedIndex < 0)
            {
                txtDetails.Text = "Выберите книгу из списка, чтобы увидеть детали...";
                return;
            }

            var books = listBooks.Tag as List<BookDoc>;
            if (books == null || listBooks.SelectedIndex >= books.Count)
            {
                return;
            }

            var book = books[listBooks.SelectedIndex];

            string details = "";
            details += "📖 " + (book.Title ?? "Без названия") + "\r\n\r\n";

            if (book.AuthorName != null && book.AuthorName.Length > 0)
            {
                details += "✍️ Автор(ы): " + string.Join(", ", book.AuthorName) + "\r\n";
            }
            else
            {
                details += "✍️ Автор: Не указан\r\n";
            }

            details += "📅 Год публикации: " + (book.FirstPublishYear?.ToString() ?? "Не указан") + "\r\n";

            if (book.NumberOfPagesMedian.HasValue)
            {
                details += "📄 Страниц: " + book.NumberOfPagesMedian + "\r\n";
            }

            if (book.Publisher != null && book.Publisher.Length > 0)
            {
                details += "🏢 Издательство: " + string.Join(", ", book.Publisher) + "\r\n";
            }

            if (book.Isbn != null && book.Isbn.Length > 0)
            {
                details += "🔢 ISBN: " + string.Join(", ", book.Isbn) + "\r\n";
            }

            if (book.Language != null && book.Language.Length > 0)
            {
                details += "🌐 Язык: " + string.Join(", ", book.Language) + "\r\n";
            }

            if (!string.IsNullOrEmpty(book.FirstSentence))
            {
                details += "\r\n📝 Краткое описание:\r\n" + book.FirstSentence + "\r\n";
            }

            if (!string.IsNullOrEmpty(book.Key))
            {
                string key = book.Key.Replace("/works/", "");
                details += "\r\n🔗 Подробнее: https://openlibrary.org/works/" + key;
            }

            txtDetails.Text = details;
        }

        // ============ МОДЕЛИ ДАННЫХ (исправлены!) ============
        public class OpenLibraryResponse
        {
            [JsonPropertyName("numFound")]
            public int NumFound { get; set; }

            [JsonPropertyName("docs")]
            public List<BookDoc> Docs { get; set; }
        }

        public class BookDoc
        {
            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("author_name")]
            public string[] AuthorName { get; set; }

            [JsonPropertyName("first_publish_year")]
            public int? FirstPublishYear { get; set; }

            [JsonPropertyName("number_of_pages_median")]
            public int? NumberOfPagesMedian { get; set; }

            [JsonPropertyName("publisher")]
            public string[] Publisher { get; set; }

            [JsonPropertyName("isbn")]
            public string[] Isbn { get; set; }

            [JsonPropertyName("language")]
            public string[] Language { get; set; }

            [JsonPropertyName("first_sentence")]
            public string FirstSentence { get; set; }

            [JsonPropertyName("key")]
            public string Key { get; set; }
        }
    }
}