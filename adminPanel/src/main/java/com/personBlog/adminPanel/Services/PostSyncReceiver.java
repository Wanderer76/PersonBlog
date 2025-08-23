package com.personBlog.adminPanel.Services;

import com.personBlog.adminPanel.Configurations.RabbitMQConfig;
import com.personBlog.adminPanel.Entities.PostBanRequest;
import com.personBlog.adminPanel.Repositories.PostBanRequestRepository;
import com.personBlog.adminPanel.Services.Models.MessagePublishWrapper;
import com.personBlog.adminPanel.Services.Models.PostBanRequestEvent;
import jakarta.validation.constraints.NotNull;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Service;

@Service
public class PostSyncReceiver {
    private final PostBanRequestRepository requestRepository;

    public PostSyncReceiver(PostBanRequestRepository requestRepository) {
        this.requestRepository = requestRepository;
    }

    @RabbitListener(queues = RabbitMQConfig.QUEUE_NAME)
    public void receiveMessage(@NotNull MessagePublishWrapper<PostBanRequestEvent> data) {
        var entity = new PostBanRequest();
        entity.setPostId(data.getEventData().getPostId());
        entity.setFilename(data.getEventData().getObjectName());
        entity.setUserMessage(data.getEventData().getUserMessage());
        entity.setReasonId(data.getEventData().getReasonId());
        entity.setCreatedAt(data.getEventData().getCreatedAt());
        entity.setCreatorUserId(data.getEventData().getCreatorUserId());
        requestRepository.save(entity);
    }
}
