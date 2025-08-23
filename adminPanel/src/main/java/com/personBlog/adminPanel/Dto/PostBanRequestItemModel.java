package com.personBlog.adminPanel.Dto;

import lombok.AllArgsConstructor;
import lombok.Getter;

import java.time.OffsetDateTime;
import java.util.UUID;

@Getter
@AllArgsConstructor
public class PostBanRequestItemModel {
    private final Long id;
    private final UUID postId;
    private final String filename;
    private final UUID reasonId;
    private final String userMessage;
    private final OffsetDateTime createdAt;
    private final  String title;
}
