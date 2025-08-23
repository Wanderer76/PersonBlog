package com.personBlog.adminPanel.Services;

import com.personBlog.adminPanel.Dto.PostBanRequestItemModel;
import com.personBlog.adminPanel.Dto.PostBanRequestsViewModel;
import com.personBlog.adminPanel.Repositories.PostBanRequestRepository;
import com.personBlog.adminPanel.Services.Models.*;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;

import java.util.UUID;
import java.util.stream.Collectors;

@Service
public class PostBanService {
    private final RabbitTemplate rabbitTemplate;
    private final PostBanRequestRepository postBanRequestRepository;
    private final PostApiService postApiService;
    private final HttpServletRequest httpServletRequest;

    public PostBanService(RabbitTemplate rabbitTemplate, PostBanRequestRepository postBanRequestRepository, PostApiService postApiService, HttpServletRequest httpServletRequest) {
        this.rabbitTemplate = rabbitTemplate;
        this.postBanRequestRepository = postBanRequestRepository;
        this.postApiService = postApiService;
        this.httpServletRequest = httpServletRequest;
    }

    public BanPostViewModel getPostsToBan(int page, int size) {
        var count = postBanRequestRepository.countActiveRequestsByPost();
        var data = postBanRequestRepository.findAllByProcessedEqualsTrueOrderByCreatedAtAsc(PageRequest
                .of(page, size));
        var token = httpServletRequest.getHeader("Authorization");

        return new BanPostViewModel(count, data.stream().parallel()
                .map(tuple -> new PostToBanItem(
                        tuple.get(0, UUID.class),
                        tuple.get(1, Long.class),
                        postApiService.getPostInfoById(tuple.get(0, UUID.class), token).get().getTitle()
                )).toList());
    }


    public PostBanRequestsViewModel getBanRequestList(UUID postId, int page, int size) {
        var count = postBanRequestRepository.countActiveRequests(postId);
        var data = postBanRequestRepository.findAllByPostIdOrderByCreatedAtAsc(postId, PageRequest
                .of(page, size));
        var token = httpServletRequest.getHeader("Authorization");
        var post = postApiService.getPostInfoById(postId, token);
        return new PostBanRequestsViewModel(count, data.stream().map(x -> {
            var title = post.isEmpty() ? "" : post.get().getTitle();
            return new PostBanRequestItemModel(x.getId(), x.getPostId(), x.getFilename(), x.getReasonId(), x.getUserMessage(), x.getCreatedAt(), title);
        }).toList());
    }

    public void sendPostBanedMessage(PostBannedEvent message) {

        var banRequestList = postBanRequestRepository.findAllByPostId(message.getPostId());
        var data = new MessagePublishWrapper<PostBannedEvent>(message, "PostBannedEvent");
        rabbitTemplate.convertAndSend("blogs", "post.banned", data);
        postBanRequestRepository.saveAll(banRequestList);
    }

    public void restorePostFromBan(PostUnBannedEvent message) {
        var banRequestList = postBanRequestRepository.findAllByPostId(message.getPostId());

        for (var i : banRequestList) {
            i.setProcessed(true);
        }
        var data = new MessagePublishWrapper(message, "PostUnBannedEvent");
        rabbitTemplate.convertAndSend("blogs", "post.unbanned", data);
    }
}


