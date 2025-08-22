package com.personBlog.adminPanel.Services.Models;

import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.UUID;

@AllArgsConstructor
@Getter
public class PostToBanItem {
    public final UUID postId;
    public final int requestCount;
}
