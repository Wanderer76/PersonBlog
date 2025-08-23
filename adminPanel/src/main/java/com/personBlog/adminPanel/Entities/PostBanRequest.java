package com.personBlog.adminPanel.Entities;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;

import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
public class PostBanRequest {
    @Id
    @GeneratedValue(strategy = GenerationType.SEQUENCE)
    @Column(name = "id", nullable = false)
    private Long id;
    private UUID postId;
    private String filename;
    private UUID reasonId;
    private UUID creatorUserId;
    private String userMessage;
    private OffsetDateTime createdAt;
    private boolean processed;
}
