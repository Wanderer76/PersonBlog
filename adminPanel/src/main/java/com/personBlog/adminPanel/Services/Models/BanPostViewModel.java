package com.personBlog.adminPanel.Services.Models;

import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.List;
import java.util.UUID;

@AllArgsConstructor
@Getter
public class BanPostViewModel {
    private final long count;
    private final List<PostToBanItem> items;
}

