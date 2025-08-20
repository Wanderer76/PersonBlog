package com.personBlog.adminPanel.Services.Models;

import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.AllArgsConstructor;
import lombok.Getter;

import java.io.Serializable;
import java.time.OffsetDateTime;
import java.util.UUID;

@AllArgsConstructor
@Getter
public class PostBannedEvent implements Serializable {
    private final String message;
    private final UUID postId;
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss.SSSXXX")
    private final OffsetDateTime createdAt;
}

