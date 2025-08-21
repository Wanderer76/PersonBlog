package com.personBlog.adminPanel.Dto;

import lombok.*;

import java.util.UUID;

@AllArgsConstructor
@NoArgsConstructor
@Getter
@Setter
public class BanActionForm {
    private UUID postId;
    private String message;
}