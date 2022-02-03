namespace Accessh.Configuration.Exception
{
    public class FilePermissionException : System.Exception
    {
        public FilePermissionException()
        {
        }

        public FilePermissionException(string message)
            : base(message)
        {
        }

        public FilePermissionException(string message, System.Exception inner)
            : base(message, inner)
        {
        }
    }
}