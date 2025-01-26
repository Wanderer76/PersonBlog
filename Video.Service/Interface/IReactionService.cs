namespace Video.Service.Interface
{
    //TODO данный сервис должен отправлять сообщения о событиях, логики быть не должно
    public interface IReactionService
    {
        Task SetReactionToPost(Guid postId);
        Task RemoveReactionToPost(Guid postId);
        Task SetViewToPost(Guid postId, Guid? userId, string segmentNumber);
    }
}
