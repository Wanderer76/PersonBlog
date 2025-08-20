package com.personBlog.adminPanel.Controllers;

import com.personBlog.adminPanel.Dto.PostBanRequest;
import com.personBlog.adminPanel.Dto.PostBanRequestsViewModel;
import com.personBlog.adminPanel.Services.Models.PostBannedEvent;
import com.personBlog.adminPanel.Services.Models.PostUnBannedEvent;
import com.personBlog.adminPanel.Services.PostBanService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;

@RestController
@RequestMapping("api/post")
public class PostController {
    private final PostBanService postBanService;

    public PostController(PostBanService postBanService) {
        this.postBanService = postBanService;
    }

    @GetMapping("banRequest/list")
    public ResponseEntity<PostBanRequestsViewModel> getBanPostRequestList(int page, int size) {
        return ResponseEntity.ok(postBanService.getBanRequestList(page, size));
    }

    @PostMapping("sendToBan")
    public void sendPostToBan(@RequestBody PostBanRequest form) {
        postBanService.sendPostBanedMessage(new PostBannedEvent(form.getMessage(), form.getPostId(), OffsetDateTime.now(ZoneOffset.UTC)));
    }

    @PostMapping("restoreFromBan")
    public void restoreFromBan(@RequestBody PostBanRequest form) {
        postBanService.restorePostFromBan(new PostUnBannedEvent(form.getPostId()));
    }
}
