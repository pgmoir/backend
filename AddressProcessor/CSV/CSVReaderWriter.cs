using System;
using System.IO;

namespace AddressProcessing.CSV
{
    /*
        2) Refactor this class into clean, elegant, rock-solid & well performing code, without over-engineering.
           Assume this code is in production and backwards compatibility must be maintained.
    */

    /* 
        The main trade off that was made in refactoring to cater for backward compatbility and the fact that our CSVReaderWriter is a public interface and could be used 
        by any application referencing it, and therefore we were limited in how to change the primary structure. Ideally it would be preferable to have one ReadFile method that returned 
        an enumerated collection of the rows split into two properties. This would mean that we could encapsulate all the reading logic with a using statement. I did 
        consider using the open method to handle the open, read and close requirements all at once, and just return items from an in memory collection. The close method would become
        redundant. Potential issue here may be the size of the file, and would holding this all in memory cause an issue. And in similar parttern, a write method which took in an array of 
        arrays or tuple elements would be safer and faster.
    */

    public class CSVReaderWriter
    {
        private StreamReader _readerStream = null;
        private StreamWriter _writerStream = null;
        private Mode _processMode;
        private const string Tab = "\t";

        [Flags]
        public enum Mode { NotSet = 0, Read = 1, Write = 2 };

        /// <summary>
        /// Initiating process, setting process mode and initialising approriate stream
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mode"></param>
        public void Open(string fileName, Mode mode)
        {
            _processMode = mode;

            switch (mode)
            {
                case Mode.Read:
                {
                    SetReaderStream(fileName);
                    break;
                }
                case Mode.Write:
                {
                    SetWriterStream(fileName);
                    break;
                }
                default:
                {
                    // it would be better not to throw exception, as this will stop multiple file processing
                    throw new Exception("Unknown file processing mode for " + fileName);
                }
            }
        }

        /// <summary>
        /// Only create a reader stream if the file actually exists, otherwise stream not created and other methods check for valid stream
        /// </summary>
        /// <param name="fileName">Name of file to be read</param>
        private void SetReaderStream(string fileName)
        {
            if (!File.Exists(fileName))
                return;

            _readerStream = File.OpenText(fileName);
        }

        /// <summary>
        /// Only create a writer stream if the filename provided is valid, and ensure that if file exists already it is removed
        /// </summary>
        /// <param name="fileName">Name of file to be created</param>
        private void SetWriterStream(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            if (File.Exists(fileName))
                File.Delete(fileName);

            var fileInfo = new FileInfo(fileName);
            _writerStream = fileInfo.CreateText();
        }

        /// <summary>
        /// Assuming that this may be called from elsewhere, need to keep public signature, but pass processing onto overloaded
        /// method
        /// </summary>
        /// <param name="column1">Empty parameter that is never used, but we can pass on to the overload as an out parameter</param>
        /// <param name="column2">Empty parameter that is never used, but we can pass on to the overload as an out parameter</param>
        /// <returns></returns>
        public bool Read(string column1, string column2)
        {
            return Read(out column1, out column2);
        }

        /// <summary>
        /// Method reads line and returns line contents as seperate strings. Only returns first two elements. Any further elements that
        /// may exist on the line would be ignored
        /// </summary>
        /// <param name="column1">First part of read line</param>
        /// <param name="column2">Second part of read line</param>
        /// <returns>Boolean result to indicate success of the request</returns>
        public bool Read(out string column1, out string column2)
        {
            var lineResults = ReadLine();

            column1 = lineResults.Item2;
            column2 = lineResults.Item3;
            return lineResults.Item1;
        }

        /// <summary>
        /// Preferred usage of returning read result and line contents. Includes validation of request, and validation of contents of read line
        /// </summary>
        /// <returns>Tuple containing item1 indicating success or failure of request, and item2 and item3 are first two elements read from line that were separated by tab</returns>
        public Tuple<bool, string, string> ReadLine()
        {
            const char tabSeparator = '\t';
            const int firstColumn = 0;
            const int secondColumn = 1;

            var falseResults = new Tuple<bool, string, string>(false, string.Empty, string.Empty);

            // If file did not exist, then do not try to process 
            if (_processMode != Mode.Read || _readerStream == null)
                return falseResults;

            var line = _readerStream.ReadLine();

            if (line == null || !line.Contains(Tab))
                return falseResults;

            var columns = line.Split(tabSeparator);
            return new Tuple<bool, string, string>(true, columns[firstColumn], columns[secondColumn]);
        }

        /// <summary>
        /// Validate writer stream status and write tab delimited column data 
        /// </summary>
        /// <param name="columns">Array of elements to write to stream using tab delimiter</param>
        public void Write(params string[] columns)
        {
            if (_processMode != Mode.Write || _writerStream == null)
                return;

            _writerStream.WriteLine(string.Join(Tab, columns));
        }

        /// <summary>
        /// Close stream based on the process mode request and whether stream was initiated
        /// Finalise clearup by clearing process mode, thus ensuring that the open method is
        /// called before processing another file
        /// </summary>
        public void Close()
        {
            if (_processMode == Mode.Write && _writerStream != null)
                _writerStream.Close();

            if (_processMode == Mode.Read && _readerStream != null)
                _readerStream.Close();

            _processMode = Mode.NotSet;
        }
    }
}
