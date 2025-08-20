package com.personBlog.adminPanel.Services.Models;

import lombok.AllArgsConstructor;
import lombok.Getter;

import java.io.Serializable;
import java.util.UUID;

@AllArgsConstructor
@Getter
public class PostUnBannedEvent implements Serializable {
    private final UUID postId;
}
