using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.DataAccess;
using System.Data.SqlClient;
using System.Configuration;
using ExelConverter.Core.Converter.Functions;
using ExelConverter.Core.DataObjects;
using Helpers;

namespace ExelConverter.Core.Settings
{
    public interface ILogIn
    {
        bool LogIn();
        bool IsLogined { get; }
    }

    public static class SettingsProvider
    {
        private static IDataAccess _appDataAccess;
        public static void Initialize(IDataAccess appDataAccess)
        {
            _appDataAccess = appDataAccess;

            AllowedCities = _appDataAccess.GetCities();
            AllowedSizes = _appDataAccess.GetSizes();
            AllowedSides = _appDataAccess.GetSidesNames();
            AllowedTypes = _appDataAccess.GetTypes();
            AllowedRegions = _appDataAccess.GetRegions();

            IniializeSettings();
        }

        public static void IniializeSettings()
        {
            CurrentSettings = SettingsObject.GetStoredSettings();
            InitializeFunctionsDescriptions();

            IsInitialized = true;
        }

        private static void InitializeFunctionsDescriptions()
        {
            FunctionDescriptions = new Dictionary<string, string>();

            FunctionDescriptions.Add("Добавить текст", @"Функция добавляет к значению ячейки или названию вкладки 
текст указанный в параметрах <Текст слева> и <Текст слева>
между ними ставится значение из параметра <Разделитель>");

            FunctionDescriptions.Add("Склеить столбцы", @"Функция склеивает значения из ячеек столбцов указанных в параметре <Названия>
через значение указанное в параметре <Разделитель> ");

            FunctionDescriptions.Add("Обрезать", @"Функция отрезает слева и справа от строки количество символов 
указанное соответственно в параметрах <Слева> и <Справа>");

            FunctionDescriptions.Add("По Умолчанию", "Функция возвращает значение заданное в параметре <Значение>");

            FunctionDescriptions.Add("Значение", "Функция возвращает значение соответствующей ячейки");

            FunctionDescriptions.Add("Поиск числа", @"Функция пытается распознать значение ячейки как число, в случае неудачи
возвращает значение из параметра <По умолчанию>");

            FunctionDescriptions.Add("Поиск значений", @"Функция ищет в содержимом ячейки одно из значений указанных в параметре <Значения>
в случае неудачи возвращает значение параметра <По умолчанию> ");

            FunctionDescriptions.Add("Цвет", "Функция возвращает цвет ячейки в шестнадцатиричном формате");

            FunctionDescriptions.Add("Гиперссылка", @"Функция возвращает гиперссылку содержащуюся в ячейке,
если ячейка не содержит гиперссылки то возвращается пустая строка");

            FunctionDescriptions.Add("Строка слева", @"Функция возвращает часть строки слева длинной указанной в параметере <Длинна>
если значение параметра <Длинна> больше длинный строки то возвращается вся строка");

            FunctionDescriptions.Add("Строка справа", @"Функция возвращает часть строки справа длинной указанной в параметере <Длинна>
если значение параметра <Длинна> больше длинный строки то возвращается вся строка");

            FunctionDescriptions.Add("Поиск размера", "Функция ищет подстроку вида <X>x<X>, <X>*<X>, <X>X<X> где <X> это число");

            FunctionDescriptions.Add("Разбиение строки", @"Функция разбивает исходную строку по символу указанному в параметре <Разделитель>,
и склеивает подстроки с номерами указанными в параметре <Индексы>");

            FunctionDescriptions.Add("Подстрока", @"Функция возвращает подстроку с началом указанном в параметре
<Начало> и длинной указанной в параметре <Длинна>");

            FunctionDescriptions.Add("Реверс строки", @"Функция переврачивает строку");

            FunctionDescriptions.Add("Замена подстроки в строке", @"Функция заменяет один набор символов на другой");

            FunctionDescriptions.Add(GetFormatedValueFunction.FunctionName, @"Функция извлекает форматированное значение из выбранной ячейки");

            FunctionDescriptions.Add(StringLengthFunction.FunctionName, "Функция возвращает '+' если длина строки удовлетворяет условиям. Если нет, то функция возвращает '-'");

            FunctionDescriptions.Add(UpperCaseFunction.FunctionName, "Функция возвращает значение в верхнем регистре");
            FunctionDescriptions.Add(LowerCaseFunction.FunctionName, "Функция возвращает значение в нижнем регистре");
            FunctionDescriptions.Add(CamelCaseFunction.FunctionName, "Функция возвращает возвращает значение, где каждое слово с заглавной буквы");
            FunctionDescriptions.Add(GetCommentFunction.FunctionName, "Функция возвращает строковый комментарий в ячейке");

            FunctionDescriptions.Add(StringContainsFunction.FunctionName, "Функция возвращает '+', если значение в ячейке содержит искомую строку, иначе возвращает '-'");

            FunctionDescriptions.Add(TrimFunction.FunctionName, "Функция возвращает значение без пробелов в начале и конце");
        }

        public static ILogIn Login { get; set; }

        public static bool DataBasesEnabled
        {
            get
            {
                if (Login != null)
                {
                    if (!Login.IsLogined)
                        try
                        {
                            if (!Login.LogIn())
                                throw new ApplicationException("Пользователь не вошёл в систему");
                        }
                        catch (Exception ex)
                        {
                            if (System.Windows.Application.Current != null)
                                System.Windows.Application.Current.Shutdown();
                            Log.AddWithCatcher("SettingsProvider.DataBasesEnabled", ex.GetExceptionText());
                        }
                    return Login.IsLogined;
                }
                return false;
            }
        }

        public static SettingsObject CurrentSettings { get; set; }

        public static Dictionary<string, string> FunctionDescriptions { get; set; }

        public static City[] AllowedCities { get; set; }

        public static Size[] AllowedSizes { get; set; }

        public static string[] AllowedSides { get; set; }

        public static DataObjects.Type[] AllowedTypes { get; set; }

        private static Region[] allowedRegions = null;
        public static Region[] AllowedRegions
        {
            get
            {
                return allowedRegions;
            }
            set
            {
                allowedRegions = value;
            }
        }

        public static string[] AllowedLights
        {
            get { return new string[] { "+", "-" }; }
        }

        public static string GetConnectionSctringValue(string str)
        {
            var csIndex = str.IndexOf("provider connection string");
            return str.Substring(csIndex + 27).Replace("\"", "");
        }


        private static bool isInitialized = false;
        public static bool IsInitialized { get { return isInitialized; } private set { isInitialized = value; } }
    }
}
