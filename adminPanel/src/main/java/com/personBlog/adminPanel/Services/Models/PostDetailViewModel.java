package com.personBlog.adminPanel.Services.Models;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.OffsetDateTime;
import java.util.UUID;

@Getter
@AllArgsConstructor
public class PostDetailViewModel {
    private final UUID id;
    private final String previewUrl;
    private final OffsetDateTime createdAt;
    private int viewCount;
    private final String description;
    private final String title;
    private int likeCount;
    private int dislikeCount;
}
