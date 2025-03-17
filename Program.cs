using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration; // NuGet: Microsoft.Extensions.Configuration, Microsoft.Extensions.Configuration.Json
using ConsoleTableExt;





class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // MARS - Multiple Active Result Sets !!! MultipleActiveResultSets=True; !!!
        string connectionString = "Server=localhost; Database=ATB2; Integrated Security=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();

                while (true) 
                {
                    Console.Clear();

                    var tablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    var tables = new List<string>();

                    using (var tableCommand = new SqlCommand(tablesQuery, connection))
                    using (var reader = tableCommand.ExecuteReader())
                    {
                        Console.WriteLine("Таблицы в базе данных:");
                        int index = 1;
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            tables.Add(tableName);
                            Console.WriteLine($"{index}. {tableName}");
                            index++;
                        }
                    }

                    Console.WriteLine("\nВведите название или номер таблицы (или 'exit' для выхода):");
                    string userInput = Console.ReadLine();

                    if (userInput.ToLower() == "exit") break; 

                    string selectedTable = "";

                    if (int.TryParse(userInput, out int tableNumber) && tableNumber > 0 && tableNumber <= tables.Count)
                    {
                        selectedTable = tables[tableNumber - 1];
                    }
                    else if (tables.Contains(userInput))
                    {
                        selectedTable = userInput;
                    }
                    else
                    {
                        Console.WriteLine("Некорректный ввод Таблица не найдена");
                        continue;
                    }

                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine($"Работа с таблицей: {selectedTable}");
                        Console.WriteLine("Выберите действие:");
                        Console.WriteLine("1 - показать структуру таблицы (список названий полей с их типами)");
                        Console.WriteLine("2 - выбрать все данные (SELECT *) с выводом на терминал");
                        Console.WriteLine("3 - вставить строку (INSERT), данные вводятся с клавиатуры");
                        Console.WriteLine("4 - обновить строку по id (UPDATE)");
                        Console.WriteLine("5 - удалить строку по id (DELETE)");
                        Console.WriteLine("6 - вернуться к списку таблиц");
                        Console.Write("Ваш выбор (1-6): ");

                        string choice = Console.ReadLine();

                        switch (choice)
                        {
                            case "1":
                                string structureQuery = $@"
                                    SELECT 
                                        COLUMN_NAME AS 'Имя поля',
                                        DATA_TYPE AS 'Тип данных',
                                        IS_NULLABLE AS 'Nullable'
                                    FROM 
                                        INFORMATION_SCHEMA.COLUMNS
                                    WHERE 
                                        TABLE_NAME = '{selectedTable}'
                                    ORDER BY 
                                        ORDINAL_POSITION";

                                using (var command = new SqlCommand(structureQuery, connection))
                                using (var structReader = command.ExecuteReader())
                                {
                                    var tableData = new List<object[]>();
                                    tableData.Add(new object[] { "Имя поля", "Тип данных", "Nullable" });

                                    while (structReader.Read())
                                    {
                                        tableData.Add(new object[] {
                                            structReader["Имя поля"],
                                            structReader["Тип данных"],
                                            structReader["Nullable"]
                                        });
                                    }

                                    ConsoleTableBuilder
                                        .From(tableData)
                                        .WithFormat(ConsoleTableBuilderFormat.Alternative)
                                        .ExportAndWriteLine();
                                }
                                break;

                            case "2":
                                string dataQuery = $"SELECT * FROM [{selectedTable}]";
                                using (var command = new SqlCommand(dataQuery, connection))
                                using (var dataReader = command.ExecuteReader())
                                {
                                    if (dataReader.HasRows)
                                    {
                                        var tableData = new List<object[]>();
                                        var headers = new object[dataReader.FieldCount];

                                        for (int i = 0; i < dataReader.FieldCount; i++)
                                        {
                                            headers[i] = dataReader.GetName(i);
                                        }
                                        tableData.Add(headers);

                                        while (dataReader.Read())
                                        {
                                            var rowData = new object[dataReader.FieldCount];
                                            dataReader.GetValues(rowData);
                                            tableData.Add(rowData);
                                        }

                                        ConsoleTableBuilder
                                            .From(tableData)
                                            .WithFormat(ConsoleTableBuilderFormat.Alternative)
                                            .ExportAndWriteLine();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Таблица не содержит данных");
                                    }
                                }
                                break;

                            case "3":
                                Console.Write("Введите значения через запятую: ");
                                string values = Console.ReadLine();
                                string insertQuery = $"INSERT INTO [{selectedTable}] VALUES ({values})";
                                using (var command = new SqlCommand(insertQuery, connection))
                                {
                                    try
                                    {
                                        int rows = command.ExecuteNonQuery();
                                        Console.WriteLine($"Добавлено записей: {rows}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Ошибка при вставке: " + ex.Message);
                                    }
                                }
                                break;

                            case "4":
                                Console.Write("Введите id записи для обновления: ");
                                string id = Console.ReadLine();
                                Console.Write("Введите обновленные значения (пример: name='новое значение', id_address=10): ");
                                string updates = Console.ReadLine();
                                string updateQuery = $"UPDATE [{selectedTable}] SET {updates} WHERE id = {id}";
                                using (var command = new SqlCommand(updateQuery, connection))
                                {
                                    try
                                    {
                                        int rows = command.ExecuteNonQuery();
                                        Console.WriteLine($"Обновлено записей: {rows}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Ошибка при обновлении: " + ex.Message);
                                    }
                                }
                                break;

                            case "5":
                                Console.Write("Введите id записи для удаления: ");
                                string deleteId = Console.ReadLine();
                                string deleteQuery = $"DELETE FROM [{selectedTable}] WHERE id = {deleteId}";
                                using (var command = new SqlCommand(deleteQuery, connection))
                                {
                                    try
                                    {
                                        int rows = command.ExecuteNonQuery();
                                        Console.WriteLine($"Удалено записей: {rows}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Ошибка при удалении: " + ex.Message);
                                    }
                                }
                                break;

                            case "6":
                                Console.Clear();
                                break;

                            default:
                                Console.WriteLine("Некорректный выбор.");
                                break;
                        }

                        Console.WriteLine("\nНажмите Enter для продолжения...");
                        Console.ReadLine();
                        if (choice == "6") break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        Console.ReadLine();
    }
}

