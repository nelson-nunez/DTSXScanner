using System;
using System.IO;
using System.Collections.Generic;

namespace ScannerdeDTSX
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseFolderPath = @"C:\Sharepoint\OneDrive - gyf.com.ar\Escritorio\DTSXs del server devsac";
            //string outputFolderPath = @"C:\Sharepoint\OneDrive - gyf.com.ar\Escritorio";
            string outputFolderPath = @"C:\Sharepoint\OneDrive - gyf.com.ar\Escritorio\DTSXs del server devsac";

            // Verificar si la ruta existe
            if (!Directory.Exists(baseFolderPath))
            {
                Console.WriteLine("La carpeta especificada no existe.");
                return;
            }

            string[] dtsxFiles = Directory.GetFiles(baseFolderPath, "*.dtsx", SearchOption.AllDirectories);
            if (dtsxFiles.Length == 0)
            {
                Console.WriteLine("No se encontraron archivos DTSX en la ruta especificada.");
                return;
            }

            foreach (string filePath in dtsxFiles)
            {
                try
                {
                    ProcessDtsxFile(filePath, outputFolderPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar el archivo {filePath}: {ex.Message}");
                }
            }

            Console.WriteLine("Proceso completado.");
        }

        static void ProcessDtsxFile(string filePath, string outputFolderPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string outputFilePath = Path.Combine(outputFolderPath, $"{fileName}_Informe.txt");

            var connectionMappings = ConnectionManagerExtractor.ExtractConnectionMappings(filePath);
            var taskGroups = TaskExtractor.ExtractTasks(filePath, connectionMappings);

            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine($"Informe para el archivo DTSX: {filePath}");
                writer.WriteLine(new string('=', 50));
                WriteTaskGroups(writer, taskGroups);
                writer.WriteLine(" ");
                writer.WriteLine("Informe finalizado.");
                writer.WriteLine(new string('=', 50));
            }

            Console.WriteLine($"Informe generado para {fileName} en {outputFilePath}");
        }

        static void WriteTaskGroups(StreamWriter writer, Dictionary<string, List<string>> taskGroups)
        {
            // Separar los grupos que contienen EventHandlers[OnError]
            var normalGroups = new Dictionary<string, List<string>>();
            var errorGroups = new Dictionary<string, List<string>>();

            foreach (var group in taskGroups)
            {
                if (group.Key.Contains("EventHandlers[OnError]"))
                {
                    errorGroups[group.Key] = group.Value;
                }
                else
                {
                    normalGroups[group.Key] = group.Value;
                }
            }

            // Escribir los grupos normales
            WriteGroups(writer, normalGroups);
            WriteGroups(writer, errorGroups);
        }

        static void WriteGroups(StreamWriter writer, Dictionary<string, List<string>> groups)
        {
            foreach (var group in groups)
            {
                writer.WriteLine($"{group.Key}:");
                foreach (var task in group.Value)
                {
                    writer.WriteLine(task);
                }
                writer.WriteLine(); // Espacio entre grupos
            }
            writer.WriteLine("========================================================================="); // Espacio entre grupos
        }
    }

    static class ConnectionManagerExtractor
    {
        public static Dictionary<string, string> ExtractConnectionMappings(string filePath)
        {
            var connectionMappings = new Dictionary<string, string>();
            string currentDtsId = null; // Almacena el DTSID actual
            bool inConnectionManager = false;

            foreach (string line in File.ReadLines(filePath))
            {
                if (line.Contains("<DTS:ConnectionManager"))
                {
                    inConnectionManager = true;
                    currentDtsId = null;
                    continue; // Saltar a la siguiente línea
                }
                if (!inConnectionManager) continue;

                // Extraer DTSID
                if (line.Contains("DTS:DTSID"))
                {
                    currentDtsId = ExtractAttributeValue(line, "DTS:DTSID");
                    continue; 
                }
                // Extraer ObjectName si hay un DTSID válido
                if (line.Contains("DTS:ObjectName") && currentDtsId != null)
                {
                    string objectName = ExtractAttributeValue(line, "DTS:ObjectName");
                    if (!string.IsNullOrEmpty(objectName))
                    {
                        connectionMappings[currentDtsId] = objectName;
                    }
                }

                // Finalizar bloque de ConnectionManager
                if (line.Contains("</DTS:ConnectionManager>"))
                {
                    inConnectionManager = false;
                }
            }
            return connectionMappings;
        }

        private static string ExtractAttributeValue(string line, string attributeName)
        {
            int startIndex = line.IndexOf(attributeName + "=\"") + attributeName.Length + 2;
            if (startIndex == -1) return null;

            int endIndex = line.IndexOf("\"", startIndex);
            if (endIndex == -1) return null;

            return line.Substring(startIndex, endIndex - startIndex);
        }
    }

    static class TaskExtractor
    {
        //public static Dictionary<string, List<string>> ExtractTasks(string filePath, Dictionary<string, string> connectionMappings)
        //{
        //    var taskGroups = new Dictionary<string, List<string>>();
        //    string refId = null;
        //    string taskName = null;
        //    string connectionId = null;
        //    string storedProcedure = null;
        //    string sequenceContainer = null;

        //    int taskCounter = 1;
        //    bool inDesignTimeBlock = false;

        //    foreach (string line in File.ReadLines(filePath))
        //    {
        //        #region Omite regiones de <DTS:DesignTimeProperties>
        //        if (line.Contains("<DTS:DesignTimeProperties>"))
        //            inDesignTimeBlock = true;
        //        if (line.Contains("</DTS:DesignTimeProperties>"))
        //            inDesignTimeBlock = false;
        //        if (inDesignTimeBlock)
        //            continue;
        //        #endregion

        //        if (line.Contains("DTS:refId"))
        //        {
        //            refId = ExtractAttributeValue(line, "DTS:refId");

        //            sequenceContainer = refId.Contains("\\") ? refId.Substring(0, refId.LastIndexOf("\\")) : "Sequence Container";
        //            sequenceContainer = sequenceContainer.Replace("Package\\", ""); // Eliminar "Package\"
        //        }

        //        if (line.Contains("DTS:ObjectName"))
        //        {
        //            taskName = ExtractAttributeValue(line, "DTS:ObjectName");
        //        }

        //        if (line.Contains("SQLTask:Connection"))
        //        {
        //            connectionId = ExtractAttributeValue(line, "SQLTask:Connection");
        //            if (connectionMappings.TryGetValue(connectionId, out string objectName))
        //            {
        //                connectionId = objectName; 
        //            }
        //        }

        //        // Buscar el stored procedure en SQLTask:SqlStatementSource
        //        if (line.Contains("SQLTask:SqlStatementSource"))
        //        {
        //            storedProcedure = ExtractAttributeValue(line, "SQLTask:SqlStatementSource");
        //        }

        //        // Buscar el stored procedure en UITypeEditor
        //        if (line.Contains("UITypeEditor=\"Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor"))
        //        {
        //            // Extraer el SP desde la línea
        //            int startIndex = line.IndexOf('>') + 1; // El carácter '>' indica el inicio del SP
        //            int endIndex = line.IndexOf("</property>", startIndex); // Encuentra el final de la propiedad
        //            if (endIndex != -1)
        //            {
        //                storedProcedure = line.Substring(startIndex, endIndex - startIndex).Trim();
        //            }
        //        }

        //        // Si se han obtenido el taskName, connectionId y storedProcedure, se agrega la tarea al grupo
        //        //if (!string.IsNullOrEmpty(taskName) || !string.IsNullOrEmpty(connectionId) && !string.IsNullOrEmpty(storedProcedure))
        //        if (!string.IsNullOrEmpty(storedProcedure))
        //        {
        //            string formattedSP = !string.IsNullOrEmpty(storedProcedure) ? FormatStoredProcedure(storedProcedure): "SIN INFO"; 
        //            AddTaskToGroup(taskGroups, sequenceContainer, taskCounter, taskName, connectionId, formattedSP);
        //            taskCounter++;

        //            // Limpiar variables después de escribir
        //            refId = null;
        //            taskName = null;
        //            connectionId = null;
        //            storedProcedure = null;
        //        }
        //    }

        //    return taskGroups;
        //}

        public static Dictionary<string, List<string>> ExtractTasks(string filePath, Dictionary<string, string> connectionMappings)
        {
            var taskGroups = new Dictionary<string, List<string>>();
            string refId = null;
            string taskName = null;
            string connectionId = null;
            string storedProcedure = null;
            string sequenceContainer = null;

            int taskCounter = 1;
            bool inDesignTimeBlock = false;

            foreach (string line in File.ReadLines(filePath))
            {
                #region Omite regiones de <DTS:DesignTimeProperties>
                if (line.Contains("<DTS:DesignTimeProperties>"))
                    inDesignTimeBlock = true;
                if (line.Contains("</DTS:DesignTimeProperties>"))
                    inDesignTimeBlock = false;
                if (inDesignTimeBlock)
                    continue;
                #endregion

                if (line.Contains("DTS:refId"))
                {
                    refId = ExtractAttributeValue(line, "DTS:refId");
                    sequenceContainer = refId.Contains("\\") ? refId.Substring(0, refId.LastIndexOf("\\")) : "Sequence Container";
                    sequenceContainer = sequenceContainer.Replace("Package\\", ""); // Eliminar "Package\"
                }

                if (line.Contains("DTS:ObjectName"))
                {
                    taskName = ExtractAttributeValue(line, "DTS:ObjectName");
                }

                if (line.Contains("SQLTask:Connection"))
                {
                    connectionId = ExtractAttributeValue(line, "SQLTask:Connection");
                    connectionId = connectionMappings.TryGetValue(connectionId, out string objectName) ? objectName : "Sin inf.";
                }

                // Buscar el stored procedure en SQLTask:SqlStatementSource
                if (line.Contains("SQLTask:SqlStatementSource"))
                {
                    storedProcedure = ExtractAttributeValue(line, "SQLTask:SqlStatementSource");
                }
                // Buscar el stored procedure en UITypeEditor
                if (line.Contains("UITypeEditor=\"Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor"))
                {
                    // Extraer el SP desde la línea
                    int startIndex = line.IndexOf('>') + 1; // El carácter '>' indica el inicio del SP
                    int endIndex = line.IndexOf("</property>", startIndex); // Encuentra el final de la propiedad
                    if (endIndex != -1)
                    {
                        storedProcedure = line.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }

                if (!string.IsNullOrEmpty(storedProcedure))
                {
                    // Si taskName o connectionId son nulos o vacíos, usarlos de la última tarea conocida
                    string effectiveTaskName = string.IsNullOrEmpty(taskName) ? "Sin inf." : taskName;
                    string effectiveConnectionId = string.IsNullOrEmpty(connectionId) ? "Sin inf." : connectionId;

                    AddTaskToGroup(taskGroups, sequenceContainer, taskCounter, effectiveTaskName, effectiveConnectionId, FormatStoredProcedure(storedProcedure));
                    taskCounter++;

                    // Limpiar solo storedProcedure
                    storedProcedure = null; 
                }
            }

            return taskGroups;
        }

        private static void AddTaskToGroup(Dictionary<string, List<string>> taskGroups, string sequenceContainer, int taskCounter, string taskName, string connectionId, string formattedSP)
        {
            string taskInfo = $"\t* Orden {taskCounter})\n" +
                              $"\t\tNombre: {taskName}\n" +
                              $"\t\tConexión: {connectionId}\n" +
                              $"\t\tSP: {formattedSP}";

            if (!taskGroups.ContainsKey(sequenceContainer))
            {
                taskGroups[sequenceContainer] = new List<string>();
            }
            taskGroups[sequenceContainer].Add(taskInfo);
        }

        private static string ExtractAttributeValue(string line, string attributeName)
        {
            int startIndex = line.IndexOf(attributeName + "=\"") + attributeName.Length + 2;
            if (startIndex == -1) return null;

            int endIndex = line.IndexOf("\"", startIndex);
            if (endIndex == -1) return null;

            return line.Substring(startIndex, endIndex - startIndex);
        }

        private static string FormatStoredProcedure(string storedProcedure)
        {
            if (storedProcedure.Contains("TRUNCATE TABLE"))
            {
                var truncateStatements = storedProcedure.Split(new[] { "TRUNCATE TABLE" }, StringSplitOptions.RemoveEmptyEntries);
                string formattedSP = "";
                foreach (var statement in truncateStatements)
                {
                    formattedSP += "\t\t\tTRUNCATE TABLE " + statement.Trim() + "\n";
                }
                return formattedSP.Trim();
            }
            else
            {
                if (!storedProcedure.TrimStart().StartsWith("exec", StringComparison.OrdinalIgnoreCase))
                {
                    return "exec " + storedProcedure.Trim();
                }
                return storedProcedure.Trim();
            }
        }
    }
}
