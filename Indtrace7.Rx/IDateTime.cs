namespace IndTrace7.Rx;

public interface IDateTime 
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}