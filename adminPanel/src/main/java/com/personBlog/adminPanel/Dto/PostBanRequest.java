package com.personBlog.adminPanel.Dto;

import lombok.*;

import java.util.UUID;

@Data
@AllArgsConstructor
@NoArgsConstructor
public class PostBanRequest {
    private UUID postId;
    private String message;
}
