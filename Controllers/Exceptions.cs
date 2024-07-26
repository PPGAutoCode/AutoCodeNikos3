
namespace ProjectName.ControllersExceptions
{
    public class BusinessException : System.Exception
    {
        public string Code { get; }
        public string Description { get; }

        public BusinessException(string code, string description)
            : base(description)
        {
            Code = code;
            Description = description;
        }
    }

    public class TechnicalException : System.Exception
    {
        public string Code { get; }
        public string Description { get; }

        public TechnicalException(string code, string description)
            : base(description)
        {
            Code = code;
            Description = description;
        }
    }
}
