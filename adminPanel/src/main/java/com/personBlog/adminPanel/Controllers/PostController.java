package com.personBlog.adminPanel.Controllers;

import com.personBlog.adminPanel.Dto.PostBanRequest;
import com.personBlog.adminPanel.Dto.PostBanRequestsViewModel;
import com.personBlog.adminPanel.Services.Models.BanPostViewModel;
import com.personBlog.adminPanel.Services.Models.PostBannedEvent;
import com.personBlog.adminPanel.Services.Models.PostUnBannedEvent;
import com.personBlog.adminPanel.Services.PostBanService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.UUID;

@RestController
@RequestMapping("api/post")
public class PostController {
    private final PostBanService postBanService;

    public PostController(PostBanService postBanService) {
        this.postBanService = postBanService;
    }

    @GetMapping("banRequest/list")
    public ResponseEntity<PostBanRequestsViewModel> getBanPostRequestList(
            @RequestParam UUID postId,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "10") int size) {
        return ResponseEntity.ok(postBanService.getBanRequestList(postId, page, size));
    }

    @GetMapping("postToBan/list")
    public ResponseEntity<BanPostViewModel> getBanPostRequestList(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "10") int size) {
            return ResponseEntity.ok(postBanService.getPostsToBan(page, size));
    }

    @PostMapping("sendToBan")
    public ResponseEntity<?> sendPostToBan(@RequestBody PostBanRequest form) {
        try {
            postBanService.sendPostBanedMessage(new PostBannedEvent(
                    form.getMessage(),
                    form.getPostId(),
                    OffsetDateTime.now(ZoneOffset.UTC)
            ));
            return ResponseEntity.ok().build();
        } catch (Exception e) {
            return ResponseEntity.badRequest().body("Ошибка: " + e.getMessage());
        }
    }

    @PostMapping("restoreFromBan")
    public ResponseEntity<?> restoreFromBan(@RequestBody PostBanRequest form) {
        try {
            postBanService.restorePostFromBan(new PostUnBannedEvent(form.getPostId()));
            return ResponseEntity.ok().build();
        } catch (Exception e) {
            return ResponseEntity.badRequest().body("Ошибка: " + e.getMessage());
        }
    }
}
