package com.personBlog.adminPanel.Dto;

import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.List;

@AllArgsConstructor
@Getter
public class PostBanRequestsViewModel {
    private final Long count;
    private final List<PostBanRequestItemModel> Items;
}
